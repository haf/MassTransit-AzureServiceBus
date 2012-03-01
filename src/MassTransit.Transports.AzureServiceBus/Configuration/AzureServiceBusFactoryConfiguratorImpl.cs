using System;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public class AzureAzureServiceBusFactoryConfiguratorImpl
		: AzureServiceBusFactoryConfigurator
	{
		public ITransportFactory Build()
		{
			//var tokenProvider = ConfigFactory.CreateTokenProvider();
			return new TransportFactoryImpl();
		}

		public void SetLockDuration(TimeSpan lockDuration)
		{
		}

		public void SetDefaultMessageTimeToLive(TimeSpan ttl)
		{
		}

		public void SetDeadLetteringOnExpiration(bool enabled)
		{
		}

		public void SetBatchedOperations(bool enabled)
		{
		}
	}

	/// <summary>
	/// Interface for users to configure the transport with
	/// </summary>
	public interface AzureServiceBusFactoryConfigurator
	{
		/// <summary>
		/// Sets the lock duration for the messages consumed from queues
		/// and topics, which affects the rate of duplicates. The lower the duration,
		/// the more duplicates you'll get for processes that never finish before
		/// the lease expires (these messages will pop up again).
		/// </summary>
		void SetLockDuration(TimeSpan lockDuration);

		/// <summary>
		/// Sets the default message time to live, i.e. how
		/// long it will remain in a queue or topic before
		/// being removed by the broker.
		/// </summary>
		void SetDefaultMessageTimeToLive(TimeSpan ttl);

		/// <summary>
		/// Sets whether the message should be moved to the built-in
		/// AZURE (not MassTransit) error poison message queue if the
		/// message is expired, i.e. if the message is NOT consumed by a 
		/// consumer.
		/// </summary>
		void SetDeadLetteringOnExpiration(bool enabled);

		/// <summary>
		/// Sets whether batched operations are enabled on
		/// topics and queues.
		/// </summary>
		void SetBatchedOperations(bool enabled);
	}
}