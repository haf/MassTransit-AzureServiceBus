using System;
using Magnum.Extensions;
using log4net;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class Subscription 
		: ConnectionBinding<ConnectionImpl>
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (Subscription));
		private readonly IEndpointAddress _address;

		UnsubscribeAction _unsubscribe;

		public Subscription(IEndpointAddress address)
		{
			if (address == null) throw new ArgumentNullException("address");
			_address = address;
		}

		public void Bind(ConnectionImpl connection)
		{
			if (Log.IsInfoEnabled)
				Log.Warn("Subscribing to {0}".FormatWith(_address.Uri.PathAndQuery));

			//var tupleTask = connection.Topics.Subscribe( 
				
			//    new SubscriptionDescriptionImpl(_address.Uri.PathAndQuery));
			//tupleTask.Wait();

			//_unsubscribe = tupleTask.Result.Item1;

			//it's better to configure the message broker to persist messages until a new client connects
			//connection.StompClient.WaitForSubscriptionConformation(_address.Uri.PathAndQuery);
		}

		public void Unbind(ConnectionImpl connection)
		{
			if (Log.IsInfoEnabled)
				Log.Warn("Unsubscribing to {0}".FormatWith(_address.Uri.PathAndQuery));

			_unsubscribe().Wait();
		}
	}
}