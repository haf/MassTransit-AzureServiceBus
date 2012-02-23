using System;
using MassTransit.Async;
using MassTransit.AzureServiceBus;
using MassTransit.Logging;
using MassTransit.Transports.AzureServiceBus.Util;
using Microsoft.ServiceBus.Messaging;

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

		Receiver _r;

		public PerConnectionReceiver([NotNull] AzureServiceBusEndpointAddress address)
		{
			if (address == null) throw new ArgumentNullException("address");
			_address = address;
		}

		public void Bind(ConnectionImpl connection)
		{
			_logger.DebugFormat("starting receiver for '{0}'", _address);
			if (_r == null) _r = ReceiverModule.StartReceiver(_address, 1000, 30);
		}

		public void Unbind(ConnectionImpl connection)
		{
			_logger.DebugFormat("stopping receiver for '{0}'", _address);

			if (_r == null) return;
			
			_r.Stop();
			((IDisposable)_r).Dispose();
		}

		public BrokeredMessage Get(TimeSpan timeout)
		{
			return _r.Get(timeout);
		}
	}
}