using System;
using MassTransit.Transports.AzureServiceBus.Receiver;
using MassTransit.Logging;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;
using TopicDescription = MassTransit.Transports.AzureServiceBus.TopicDescription;

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
		readonly Action<Receiver.Receiver> _onBound;
        readonly Action<Receiver.Receiver> _onUnbound;
		Receiver.Receiver _receiver;

		public PerConnectionReceiver(
			[NotNull] AzureServiceBusEndpointAddress address, 
			[NotNull] ReceiverSettings settings,
            [NotNull] Action<Receiver.Receiver> onBound,
            [NotNull] Action<Receiver.Receiver> onUnbound)
		{
			if (address == null) throw new ArgumentNullException("address");
			if (onBound == null) throw new ArgumentNullException("onBound");
			if (onUnbound == null) throw new ArgumentNullException("onUnbound");

			_address = address;
			_settings = settings;
			_onBound = onBound;
			_onUnbound = onUnbound;
		}

		/// <summary>
		/// Normal Receiver is started
		/// </summary>
		public void Bind(ConnectionImpl connection)
		{
			_logger.DebugFormat("starting receiver for '{0}'", _address);

			if (_receiver != null)
				return;
			
			_receiver = ReceiverModule.StartReceiver(_address, _settings);
			_receiver.Error += (sender, args) => _logger.Error("Error from receiver", args.Exception);
			_onBound(_receiver);
		}

		/// <summary>
		/// Normal Receiver is stopped/disposed
		/// </summary>
		public void Unbind(ConnectionImpl connection)
		{
			_logger.DebugFormat("stopping receiver for '{0}'", _address);

			if (_receiver == null)
				return;

			_onUnbound(_receiver);

			((IDisposable)_receiver).Dispose();
			_receiver = null;
		}

		/// <summary>
		/// Get a message from the queue (in memory one)
		/// </summary>
		public BrokeredMessage Get(TimeSpan timeout)
		{
			if (_receiver == null) 
				throw new ApplicationException("Call Bind before calling Get");

			return _receiver.Get(timeout);
		}
	}
}