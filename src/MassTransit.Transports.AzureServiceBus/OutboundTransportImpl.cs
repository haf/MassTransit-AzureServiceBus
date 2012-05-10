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
using System.IO;
using System.Threading;
using Magnum.Extensions;
using MassTransit.Async;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Internal;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus
{
	/// <summary>
	/// 	Outbound transport targeting the azure service bus.
	/// </summary>
	public class OutboundTransportImpl
		: IOutboundTransport
	{
		const string NumberOfRetries = "busy-retries";
		static readonly ILog _logger = Logger.Get(typeof (OutboundTransportImpl));

		bool _disposed;

		int _messagesInFlight;
		int _sleeping;

		readonly AzureServiceBusEndpointAddress _address;
		readonly ConnectionHandler<ConnectionImpl> _connectionHandler;
		readonly SenderSettings _settings;

		/// <summary>
		/// 	c'tor
		/// </summary>
		public OutboundTransportImpl(
			[NotNull] AzureServiceBusEndpointAddress address,
			[NotNull] ConnectionHandler<ConnectionImpl> connectionHandler, 
			[NotNull] SenderSettings settings)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (connectionHandler == null) throw new ArgumentNullException("connectionHandler");
			if (settings == null) throw new ArgumentNullException("settings");

			_connectionHandler = connectionHandler;
			_settings = settings;
			_address = address;

			_logger.DebugFormat("created outbound transport for address '{0}'", address);
		}

		public void Dispose()
		{
			if (_disposed) return;
			try
			{
				_address.Dispose();
				_connectionHandler.Dispose();
			}
			finally
			{
				_disposed = true;
			}
		}

		/// <summary>
		/// 	Gets the endpoint address this transport sends to.
		/// </summary>
		public IEndpointAddress Address
		{
			get { return _address; }
		}

		// service bus best practices for performance:
		// http://msdn.microsoft.com/en-us/library/windowsazure/hh528527.aspx
		public void Send(ISendContext context)
		{
			_connectionHandler
				.Use(connection =>
					{
						// don't have too many outstanding at same time
						SpinWait.SpinUntil(() => _messagesInFlight < _settings.MaxOutstanding);

						using (var body = new MemoryStream())
						{
							context.SerializeTo(body);
							
							// the envelope is re-usable, so let's capture it in the below closure
							// as a value
							var envelope = new MessageEnvelope(body.ToArray());

                            Retries.Retry(FaultPolicies.FinalAzurePolicy, () =>
                            {
							    SendMessage(connection, () =>
								{
									var brokeredMessage = new BrokeredMessage(envelope);

									if (!string.IsNullOrWhiteSpace(context.CorrelationId))
										brokeredMessage.CorrelationId = context.CorrelationId;

									if (!string.IsNullOrWhiteSpace(context.MessageId))
										brokeredMessage.MessageId = context.MessageId;
									
									return brokeredMessage;
								}, 1);
                            }).Wait();
						}
					});
		}


		void SendMessage(ConnectionImpl connection, Func<BrokeredMessage> createMessage, int sendNumber)
		{
			var msg = createMessage();
			var messageId = msg.MessageId;
			var sender = connection.MessageSender;

			msg.Properties[NumberOfRetries] = sendNumber - 1;

			Address.LogBeginSend(msg.MessageId);

			Interlocked.Increment(ref _messagesInFlight);

			connection.MessageSender.BeginSend(msg, ar =>
			{
				// if the queue is deleted in the middle of things here, then I can't recover
				// at the moment; I have to extend the connection handler with an asynchronous
				// API to let it re-initialize the queue and hence maybe even the full transport...

				// So if I get MessagingEntityNotFoundException, I'm toast with this code: 
				// don't delete queues in use.

				Interlocked.Decrement(ref _messagesInFlight);

                try
                {
                    sender.EndSend(ar);
                    Address.LogEndSend(msg.MessageId);
                }
                finally
                {
				    // always dispose the message; it's only good once
				    msg.Dispose();                        
                }
			}, null);
		}
	}
}