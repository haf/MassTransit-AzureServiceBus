// Copyright 2012 Henrik Feldt
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using System;
using Magnum.Extensions;
using Magnum.Threading;
using MassTransit.Exceptions;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Configuration;
using MassTransit.Transports.AzureServiceBus.Management;
using MassTransit.Util;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// 	Implementation of the transport factory
	/// </summary>
	public class TransportFactoryImpl
		: ITransportFactory
	{
		static readonly ILog _logger = Logger.Get(typeof (TransportFactoryImpl));

		readonly ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>> _connCache;
		readonly ReaderWriterLockedDictionary<Uri, AzureServiceBusEndpointAddress> _addresses;
		readonly AzureMessageNameFormatter _formatter;

		bool _disposed;

		readonly ReceiverSettings _receiverSettings;
		readonly SenderSettings _senderSettings;

		/// <summary>
		/// 	c'tor
		/// </summary>
		public TransportFactoryImpl()
			: this(new ReceiverSettingsImpl(), new SenderSettingsImpl())
		{
		}

		/// <summary>
		/// 	c'tor taking settings to configure the endpoint with
		/// </summary>
		public TransportFactoryImpl(ReceiverSettings receiverSettings, SenderSettings senderSettings)
		{
			_addresses = new ReaderWriterLockedDictionary<Uri, AzureServiceBusEndpointAddress>();
			_connCache = new ReaderWriterLockedDictionary<Uri, ConnectionHandler<ConnectionImpl>>();
			_formatter = new AzureMessageNameFormatter();

			_receiverSettings = receiverSettings;
			_senderSettings = senderSettings;

			_logger.Debug("created new transport factory");
		}

		/// <summary>
		/// 	Gets the scheme. (af-queues)
		/// </summary>
		public string Scheme
		{
			get { return Constants.Scheme; }
		}

		/// <summary>
		/// 	The message name formatter associated with this transport
		/// </summary>
		public IMessageNameFormatter MessageNameFormatter
		{
			get { return _formatter; }
		}

		/// <summary>
		/// 	Builds the duplex transport.
		/// </summary>
		/// <param name="settings"> The settings. </param>
		/// <returns> </returns>
		public IDuplexTransport BuildLoopback([NotNull] ITransportSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			_logger.Debug("building duplex transport");

			return new Transport(settings.Address, 
				() => BuildInbound(settings), () => BuildOutbound(settings));
		}

		/// <summary>
		/// 	Builds the inbound transport for the service bus endpoint,
		/// </summary>
		/// <param name="settings"> using these settings </param>
		/// <returns> A non-null instance of the inbound transport. </returns>
		public virtual IInboundTransport BuildInbound([NotNull] ITransportSettings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");

			EnsureProtocolIsCorrect(settings.Address.Uri);

			var address = _addresses.Retrieve(settings.Address.Uri,
			                                  () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			_logger.DebugFormat("building inbound transport for address '{0}'", address);

			var client = GetConnection(address);

			return new InboundTransportImpl(address, client, new PurgeImpl(settings.PurgeExistingMessages, address),
			                                MessageNameFormatter, _receiverSettings);
		}

		/// <summary>
		/// 	Builds the outbound transport
		/// </summary>
		/// <param name="settings"> with settings </param>
		/// <returns> The outbound transport instance, non-null </returns>
		public virtual IOutboundTransport BuildOutbound([NotNull] ITransportSettings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var address = _addresses.Retrieve(settings.Address.Uri,
			                                  () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			_logger.DebugFormat("building outbound transport for address '{0}'", address);

			var client = GetConnection(address);

			return new OutboundTransportImpl(address, client, _senderSettings);
		}

		/// <summary>
		/// 	Builds the outbound error transport; where to send messages that fail.
		/// </summary>
		/// <param name="settings"> using these settings </param>
		/// <returns> The outbound transport instance, non null </returns>
		public virtual IOutboundTransport BuildError([NotNull] ITransportSettings settings)
		{
			if (settings == null) throw new ArgumentNullException("settings");
			EnsureProtocolIsCorrect(settings.Address.Uri);

			var address = _addresses.Retrieve(settings.Address.Uri,
			                                  () => AzureServiceBusEndpointAddressImpl.Parse(settings.Address.Uri));
			_logger.DebugFormat("building error transport for address '{0}'", address);

			var client = GetConnection(address);
			return new OutboundTransportImpl(address, client, _senderSettings);
		}

		/// <summary>
		/// 	Ensures the protocol is correct.
		/// </summary>
		/// <param name="address"> The address. </param>
		void EnsureProtocolIsCorrect([NotNull] Uri address)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (address.Scheme != Scheme)
				throw new EndpointException(address,
				                            string.Format("Address must start with 'stomp' not '{0}'", address.Scheme));
		}

		ConnectionHandler<ConnectionImpl> GetConnection([NotNull] AzureServiceBusEndpointAddress address)
		{
			if (address == null) throw new ArgumentNullException("address");
			_logger.DebugFormat("get connection( address:'{0}' )", address);

			return _connCache.Retrieve(address.Uri, () =>
				{
					var connection = new ConnectionImpl(address);
					var connectionHandler = new ConnectionHandlerImpl<ConnectionImpl>(connection);

					return connectionHandler;
				});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool managed)
		{
			if (_disposed)
				return;

			if (managed)
			{
				_connCache.Values.Each(x => x.Dispose());
				_connCache.Clear();
				_connCache.Dispose();

				_addresses.Values.Each(x => x.Dispose());
				_addresses.Clear();
				_addresses.Dispose();
			}

			_disposed = true;
		}
	}
}