using System;
using Magnum.Extensions;
using log4net;

namespace MassTransit.Transports.AzureQueue
{
	public class ServiceBusQueueSubsciption :
		ConnectionBinding<ServiceBusQueuesConnection>
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (ServiceBusQueueSubsciption));
		private readonly IEndpointAddress _address;

		public ServiceBusQueueSubsciption(IEndpointAddress address)
		{
			if (address == null) throw new ArgumentNullException("address");
			_address = address;
		}

		public void Bind(ServiceBusQueuesConnection connection)
		{
			if (Log.IsInfoEnabled)
				Log.Warn("Subscribing to {0}".FormatWith(_address.Uri.PathAndQuery));

			//connection.StompClient.Subscribe(_address.Uri.PathAndQuery);

			//it's better to configure the message broker to persist messages until a new client connects
			//connection.StompClient.WaitForSubscriptionConformation(_address.Uri.PathAndQuery);
		}

		public void Unbind(ServiceBusQueuesConnection connection)
		{
			if (Log.IsInfoEnabled)
				Log.Warn("Unsubscribing to {0}".FormatWith(_address.Uri.PathAndQuery));

			//connection.StompClient.Unsubscribe(_address.Uri.PathAndQuery);
		}
	}
}