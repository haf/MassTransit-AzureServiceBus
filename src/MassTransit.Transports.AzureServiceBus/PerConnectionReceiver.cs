using System;
using System.Collections.Generic;
using MassTransit.Async;
using MassTransit.AzureServiceBus;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;
using TopicDescription = MassTransit.AzureServiceBus.TopicDescription;

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// Tiny wrapper around the F# receiver that is started only when the connection is
	/// bound and not before. The reason for this is that the receiver needs the management
	/// to do its work before becoming active. Possibly, the interface and hence implementation
	/// that calls the start and stop methods could be moved to the F# project.
	/// </summary>
	class PerConnectionReceiver
		: ConnectionBinding<ConnectionImpl>
	{
		readonly AzureServiceBusEndpointAddress _address;
		static readonly ILog _logger = Logger.Get(typeof (PerConnectionReceiver));

		readonly ReceiverSettings _settings;
		readonly Action<Receiver> _onBound;
		readonly Action<Receiver> _onUnbound;
		Receiver _receiver;

		public PerConnectionReceiver(
			[NotNull] AzureServiceBusEndpointAddress address, 
			[CanBeNull] ReceiverSettings settings, 
			[NotNull] Action<Receiver> onBound, 
			[NotNull] Action<Receiver> onUnbound)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (onBound == null) throw new ArgumentNullException("onBound");
			if (onUnbound == null) throw new ArgumentNullException("onUnbound");

			_address = address;
			_settings = settings;
			_onBound = onBound;
			_onUnbound = onUnbound;
		}

		public void Bind(ConnectionImpl connection)
		{
			_logger.DebugFormat("starting receiver for '{0}'", _address);

			if (_receiver != null) 
				return;
			
			_receiver = ReceiverModule.StartReceiver(_address, _settings);
			_onBound(_receiver);
		}

		public void Unbind(ConnectionImpl connection)
		{
			_logger.DebugFormat("stopping receiver for '{0}'", _address);

			if (_receiver == null)
				return;

			_onUnbound(_receiver);

			((IDisposable)_receiver).Dispose();
			_receiver = null;
		}

		public BrokeredMessage Get(TimeSpan timeout)
		{
			if (_receiver == null) 
				throw new ApplicationException("Call Bind before calling Get");

			return _receiver.Get(timeout);
		}

		public void SubscribeTopic([NotNull] TopicDescription value)
		{
			if (value == null) throw new ArgumentNullException("value");
			_receiver.Subscribe(value);
		}

		public void UnsubscribeTopic([NotNull] TopicDescription value)
		{
			if (value == null) throw new ArgumentNullException("value");
			_receiver.Unsubscribe(value);
		}
	}
}