using System;
using MassTransit.AzureServiceBus;
using MassTransit.AzureServiceBus.Util;
using MassTransit.Pipeline.Inspectors;
using MassTransit.Pipeline.Sinks;

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
	/// <summary>
	/// Finds publish endpoints that consume the message type specified in the c'tor of this
	/// class.
	/// </summary>
	public class PublishEndpointSinkLocator :
		PipelineInspectorBase<PublishEndpointSinkLocator>
	{
		readonly AzureServiceBusEndpointAddress _endpointAddress;
		readonly Type _messageType;

		/// <summary>
		/// c'tor
		/// </summary>
		public PublishEndpointSinkLocator([NotNull] Type messageType, [NotNull] AzureServiceBusEndpointAddress endpointAddress)
		{
			if (messageType == null) throw new ArgumentNullException("messageType");
			if (endpointAddress == null) throw new ArgumentNullException("endpointAddress");
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