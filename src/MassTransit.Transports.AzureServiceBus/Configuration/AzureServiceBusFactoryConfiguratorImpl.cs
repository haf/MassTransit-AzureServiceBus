namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public class AzureAzureServiceBusFactoryConfiguratorImpl
		: AzureServiceBusFactoryConfigurator
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
	public interface AzureServiceBusFactoryConfigurator
	{
		/// <summary>
		/// Sets the default subscription options for this transport.
		/// </summary>
		void SetDefaultSubscriptionDescription(
			SubscriptionDescription defaultOptions);
	}
}