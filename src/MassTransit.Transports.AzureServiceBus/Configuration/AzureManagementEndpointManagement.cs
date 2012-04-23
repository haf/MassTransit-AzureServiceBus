using System;
using System.Collections.Generic;
using MassTransit.Async;
using MassTransit.AzureServiceBus;

#pragma warning disable 1591

namespace MassTransit.Transports.AzureServiceBus.Configuration
{
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
}