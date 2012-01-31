namespace MassTransit.Transports.ServiceBusQueues.Configuration
{
	public class ServiceBusQueuesFactoryConfiguratorImpl
		: ServiceBusQueuesFactoryConfigurator
	{
		SubscriptionDescription _defaultSub;

		public ITransportFactory Build()
		{
			//var tokenProvider = ConfigFactory.CreateTokenProvider();
			return new TransportFactoryImpl();
		}

		public void SetDefaultSubscriptionDescription(SubscriptionDescription defaultOptions)
		{
			_defaultSub = defaultOptions;
		}
	}

	/// <summary>
	/// Interface for users to configure the transport with
	/// </summary>
	public interface ServiceBusQueuesFactoryConfigurator
	{
		/// <summary>
		/// Sets the default subscription options for this transport.
		/// </summary>
		void SetDefaultSubscriptionDescription(
			SubscriptionDescription defaultOptions);
	}
}