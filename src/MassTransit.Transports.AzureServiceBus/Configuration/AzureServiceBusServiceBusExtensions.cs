using System;
using System.Collections.Generic;
using Magnum.Extensions;
using Magnum.Reflection;
using MassTransit.Async;
using MassTransit.AzureServiceBus;
using MassTransit.AzureServiceBus.Util;
using MassTransit.BusConfigurators;
using MassTransit.Exceptions;
using MassTransit.Pipeline.Configuration;
using MassTransit.Pipeline.Inspectors;
using MassTransit.Pipeline.Sinks;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	public static class AzureServiceBusServiceBusExtensions
	{
		/// <summary>
		/// Specifies that MT should be using AppFabric ServiceBus Queues to receive messages and specifies the
		/// uri by means of its components.
		/// </summary>
		public static void ReceiveFromComponents<T>(this T configurator, 
			string issuerOrUsername, 
			string defaultKeyOrPassword, string serviceBusNamespace,
			string application)
			where T : ServiceBusConfigurator
		{
			var credentials = new Credentials(issuerOrUsername, defaultKeyOrPassword, serviceBusNamespace, application);
			configurator.ReceiveFrom(credentials.BuildUri());
		}

		[UsedImplicitly] // in public API
		public static void ReceiveFromComponents<T>(this T configurator,
			PreSharedKeyCredentials creds)
			where T : ServiceBusConfigurator
		{
			configurator.ReceiveFrom(creds.BuildUri());
		}

		/// <summary>
		/// Configure the service bus to use the queues and topics routing semantics with
		/// Azure ServiceBus.
		/// </summary>
		public static void UseAzureServiceBusRouting<T>(this T configurator)
			where T : ServiceBusConfigurator
		{
			configurator.SetSubscriptionObserver((sb, router) =>
				{
					var inboundTransport = sb.Endpoint.InboundTransport.CastAs<InboundTransportImpl>();
					return new TopicSubscriptionObserver(inboundTransport.MessageNameFormatter, inboundTransport);
				});

			var busConf = new PostCreateBusBuilderConfigurator(bus =>
				{
					var interceptorConf = new OutboundMessageInterceptorConfigurator(bus.OutboundPipeline);

					interceptorConf.Create(new PublishEndpointInterceptor(bus));
				});

			configurator.AddBusConfigurator(busConf);

			// configurator.UseAzureServiceBus();
		}
	}

	public class PublishEndpointInterceptor : IOutboundMessageInterceptor
	{
		readonly ServiceBus _bus;
		readonly IMessageNameFormatter _formatter;
		readonly AzureServiceBusEndpointAddress _address;
		readonly Dictionary<Type, MassTransit.UnsubscribeAction> _added;

		public PublishEndpointInterceptor([NotNull] ServiceBus bus)
		{
			if (bus == null) throw new ArgumentNullException("bus");

			_bus = bus;

			var inbound = bus.Endpoint.InboundTransport as InboundTransportImpl;

			if (inbound == null)
				throw new ConfigurationException(
					"The bus must be configured to receive from an Azure ServiceBus Endpoint for this interceptor to work.");

			_formatter = inbound.MessageNameFormatter;
			_address = inbound.Address.CastAs<AzureServiceBusEndpointAddress>();
			_added = new Dictionary<Type, MassTransit.UnsubscribeAction>();
		}

		void IOutboundMessageInterceptor.PreDispatch(ISendContext context)
		{
			lock (_added)
			{
				var messageType = context.DeclaringMessageType;

				if (_added.ContainsKey(messageType)) 
					return;

				AddEndpointForType(messageType);
			}
		}

		void IOutboundMessageInterceptor.PostDispatch(ISendContext context)
		{
		}

		/// <summary>
		/// Adds an endpoint for the message type. This will look up all super-classes
		/// of the message's type (running for those as well) and then create
		/// message sinks corresponding to the type of message that is being published.
		/// </summary>
		/// <param name="messageType">The type of message to add an endpoint for.</param>
		void AddEndpointForType(Type messageType)
		{
			using (var management = new AzureManagementEndpointManagement(_address))
			{
				var types = management.CreateTopicsForPublisher(messageType, _formatter);

				foreach (var type in types)
				{
					if (_added.ContainsKey(type))
						continue;

					var messageName = _formatter.GetMessageName(type);

					var messageEndpointAddress = _address.ForTopic(messageName.ToString());

					FindOrAddEndpoint(type, messageEndpointAddress);
				}
			}
		}

		/// <summary>
		/// Finds all endpoints in the outbound pipeline and starts routing messages
		/// to that endpoint.
		/// </summary>
		/// <param name="messageType">type of message</param>
		/// <param name="address">The message endpoint address.</param>
		void FindOrAddEndpoint(Type messageType, AzureServiceBusEndpointAddress address)
		{
			var locator = new PublishEndpointSinkLocator(messageType, address);
			_bus.OutboundPipeline.Inspect(locator);

			if (locator.Found)
			{
				_added.Add(messageType, () => true);
				// subscribed sink exists already, returning
				return;
			}

			// otherwise, get the endpoint and add a sink for it
			var endpoint = _bus.GetEndpoint(address.Uri);

			this.FastInvoke(new[] {messageType}, "CreateEndpointSink", endpoint);
		}

		/// <summary>
		/// Actually create a new sink; the sink didn't exist in the outbound
		/// pipeline, so we need to create a new one.
		/// </summary>
		/// <typeparam name="TMessage">Message type to create the sink for.</typeparam>
		/// <param name="endpoint">The endpoint to attach the message sink to.</param>
		[UsedImplicitly]
		void CreateEndpointSink<TMessage>(IEndpoint endpoint)
			where TMessage : class
		{
			var endpointSink = new EndpointMessageSink<TMessage>(endpoint);

			var filterSink = new OutboundMessageFilter<TMessage>(endpointSink,
				context => context.DeclaringMessageType == typeof (TMessage));

			var unsub = _bus.OutboundPipeline.ConnectToRouter(filterSink);

			_added.Add(typeof (TMessage), unsub);
		}
	}

	/// <summary>
	/// Extensible class for managing an azure endpoint/topics. A single virtual method
	/// that can be overridden, the <see cref="CreateTopicsForPublisher"/> (and <see cref="Dispose(bool)"/>).
	/// </summary>
	public class AzureManagementEndpointManagement : IDisposable
	{
		readonly AzureServiceBusEndpointAddress _address;

		public AzureManagementEndpointManagement(AzureServiceBusEndpointAddress address)
		{
			_address = address;
		}

		public virtual IEnumerable<Type> CreateTopicsForPublisher(Type messageType, IMessageNameFormatter formatter)
		{
			var nm = _address.NamespaceManager;

			foreach (var type in messageType.GetMessageTypes())
			{
				var topic = formatter.GetMessageName(type).ToString();
				
				/*
				 * Type here is both the actual message type and its 
				 * interfaces. In RMQ we could bind the interface type
				 * exchanges to the message type exchange, but because this
				 * is azure service bus, we only have plain topics.
				 * 
				 * This means that for a given subscribed message, we have to
				 * subscribe also to all of its interfaces and their corresponding
				 * topics. In this method, it means that we'll just create
				 * ALL of the types' corresponding topics and publish to ALL of them.
				 * 
				 * On the receiving side we'll have to de-duplicate 
				 * the received messages, potentially (unless we're subscribing only
				 * to one of the interfaces that itself doesn't implement any interfaces).
				 */

				nm.CreateAsync(new TopicDescriptionImpl(topic)).Wait();

				yield return type;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool managed)
		{
		}
	}

	/// <summary>
	/// Finds publish endpoints that consume the message type specified in the c'tor of this
	/// class.
	/// </summary>
	public class PublishEndpointSinkLocator :
		PipelineInspectorBase<PublishEndpointSinkLocator>
	{
		readonly AzureServiceBusEndpointAddress _endpointAddress;
		readonly Type _messageType;

		public PublishEndpointSinkLocator(Type messageType, AzureServiceBusEndpointAddress endpointAddress)
		{
			_endpointAddress = endpointAddress;
			_messageType = messageType;
		}

		/// <summary>
		/// Gets whether the publish endpoint sink locator found a message sink
		/// that matched the type passed in the c'tor.
		/// </summary>
		public bool Found { get; private set; }

		/// <summary>
		/// Called by the reflective visitor base.
		/// </summary>
		public bool Inspect<TMessage>(EndpointMessageSink<TMessage> sink)
			where TMessage : class
		{
			if (typeof(TMessage) == _messageType && _endpointAddress.Uri == sink.Endpoint.Address.Uri)
			{
				Found = true;

				return false;
			}

			return true;
		}
	}

}