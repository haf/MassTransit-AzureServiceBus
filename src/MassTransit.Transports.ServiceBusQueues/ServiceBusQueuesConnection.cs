using System;
using Magnum.Extensions;
using MassTransit.Transports.ServiceBusQueues.Utils;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class ServiceBusQueuesConnection :
		Connection
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof (ServiceBusQueuesConnection));
		private readonly Uri _serviceUri;
		private bool _disposed;
		private object _someClient;
		private QueueClient _queue;

		public ServiceBusQueuesConnection([NotNull] Uri serviceUri)
		{
			if (serviceUri == null) throw new ArgumentNullException("serviceUri");
			_serviceUri = serviceUri;
		}

		public QueueClient Queue
		{
			get { return _queue; }
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool managed)
		{
			if (!managed)
				return;

			if (_disposed)
				throw new ObjectDisposedException("ServiceBusQueueConnection for {0}".FormatWith(
					_serviceUri),
					"The connection instance to AppFabric ServiceBus Queues, " +
					"is already disposed and cannot be disposed twice.");
			try
			{
				Disconnect();
			}
			finally
			{
				_disposed = true;
			}
		}

		public void Connect()
		{
			Disconnect();

			var serverAddress = new UriBuilder("ws", _serviceUri.Host, _serviceUri.Port).Uri;

			_log.Info("Connecting {0}".FormatWith(_serviceUri));
		}

		public void Disconnect()
		{
			try
			{
				if (_someClient != null)
				{
					_log.Info("Disconnecting {0}".FormatWith(_serviceUri));

					//if (_stompClient.IsConnected)
					//    _stompClient.Disconnect();

					//_stompClient.Dispose();
					//_stompClient = null;
				}
			}
			catch (Exception ex)
			{
				_log.Warn("Failed to close AppFabric ServiceBus connection.", ex);
			}
		}
	}
}