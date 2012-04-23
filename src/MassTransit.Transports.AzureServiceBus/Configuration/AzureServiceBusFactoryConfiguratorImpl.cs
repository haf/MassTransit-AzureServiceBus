using System;
using MassTransit.AzureServiceBus.Util;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
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

		/// <summary>
		/// Set the receiver name (this corresponds to the name of the subscription created on topics
		/// in Azure ServiceBus. Setting this equal to what another bus has, allows your
		/// bus to do competing consumes on all message types that it consumes.
		/// </summary>
		/// <exception cref="ArgumentException">name.trim() is an empty string</exception>
		/// <param name="name">Name to use for subscriptions.</param>
		void SetReceiverName(string name);

		/// <summary>
		/// Sets the timeout for receiving a message using a single operation.
		/// </summary>
		void SetReceiveTimeout(TimeSpan timeout);
	}

#pragma warning disable 1591

	/// <summary>
	/// See <see cref="AzureServiceBusFactoryConfigurator"/>.
	/// </summary>
	public class AzureAzureServiceBusFactoryConfiguratorImpl
		: AzureServiceBusFactoryConfigurator
	{
		private readonly ReceiverSettingsImpl _settings = new ReceiverSettingsImpl();

		static Logging.ILog _logger = Logging.Logger.Get<AzureAzureServiceBusFactoryConfiguratorImpl>();

		/// <summary>
		/// Actually build the transport factory.
		/// </summary>
		/// <returns>An instance of the transport factory</returns>
		[NotNull]
		public ITransportFactory Build()
		{
			_logger.Debug("building transport factory");
			//var tokenProvider = ConfigFactory.CreateTokenProvider();
			return new TransportFactoryImpl(_settings);
		}

		public void SetLockDuration(TimeSpan lockDuration)
		{
			MissingSetting("SetLockDuration");
		}

		public void SetDefaultMessageTimeToLive(TimeSpan ttl)
		{
			MissingSetting("SetDefaultMessageTimeToLive");
		}

		public void SetDeadLetteringOnExpiration(bool enabled)
		{
			MissingSetting("SetDeadLetteringOnExpiration");
		}

		public void SetBatchedOperations(bool enabled)
		{
			MissingSetting("SetBatchedOperations");
		}

		static void MissingSetting(string setting)
		{
			_logger.Warn(string.Format("Setting {0} is currently not implemented", setting));
		}

		public void SetReceiveTimeout(TimeSpan timeout)
		{
			_logger.DebugFormat("setting ReceiveTimeout to {0}", timeout);
			_settings.ReceiveTimeout = timeout;
		}

		public void SetReceiverName([NotNull] string name)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (name.Trim() == "") throw new ArgumentException("name mustn't be empty", "name");

			_logger.DebugFormat("setting ReceiverName to {0}", name);
			_settings.ReceiverName = name;
		}
	}
}