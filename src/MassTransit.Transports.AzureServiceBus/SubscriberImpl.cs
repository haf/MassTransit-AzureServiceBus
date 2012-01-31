using System;
using System.Threading.Tasks;
using MassTransit.Util;
using Microsoft.ServiceBus.Messaging;
using log4net;

namespace MassTransit.Transports.AzureServiceBus
{
	public class SubscriberImpl
		: Subscriber, ConnectionBinding<ConnectionImpl>
	{
		static readonly ILog _logger = LogManager.GetLogger(typeof (SubscriberImpl));
		
		readonly MessageReceiver _receiver;
		readonly SubscriptionClient _client;

		public SubscriberImpl([NotNull] MessageReceiver receiver)
		{
			if (receiver == null) throw new ArgumentNullException("receiver");
			_receiver = receiver;
		}

		public SubscriberImpl([NotNull] SubscriptionClient receiver)
		{
			if (receiver == null) throw new ArgumentNullException("receiver");
			_client = receiver;
		}

		void ConnectionBinding<ConnectionImpl>.Bind(ConnectionImpl connection)
		{
			throw new NotImplementedException();
		}

		void ConnectionBinding<ConnectionImpl>.Unbind(ConnectionImpl connection)
		{
			throw new NotImplementedException();
		}

		public Task<BrokeredMessage> Receive()
		{
			_logger.Debug("begin receive");
			return (_client == null
			        	? Task.Factory.FromAsync<BrokeredMessage>(
			        		_receiver.BeginReceive,
			        		_receiver.EndReceive, null)
			        	: Task.Factory.FromAsync<BrokeredMessage>(
			        		_client.BeginReceive,
			        		_client.EndReceive, null))
				.ContinueWith(tRec =>
					{
						_logger.Debug("end receive");
						return tRec.Result;
					});
		}

		public Task<BrokeredMessage> Receive(TimeSpan timeout)
		{
			_logger.Debug(string.Format("begin receive w/ timespan {0}", timeout));
			return (_client == null
			        	? Task.Factory.FromAsync<TimeSpan, BrokeredMessage>(
			        		_receiver.BeginReceive,
			        		_receiver.EndReceive, timeout, null)
			        	: Task.Factory.FromAsync<TimeSpan, BrokeredMessage>(
			        		_client.BeginReceive,
			        		_client.EndReceive, timeout, null))
				.ContinueWith(tRec =>
					{
						_logger.Debug("end receive w/ timespan");
						return tRec.Result;
					});
		}
	}
}