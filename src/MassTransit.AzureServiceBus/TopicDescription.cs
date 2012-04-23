using System;

namespace MassTransit.AzureServiceBus
{
	/// <summary>
	/// value object, implementing equality of <see cref="PathBasedEntity.Path"/>.
	/// </summary>
	public interface TopicDescription
		: IEquatable<TopicDescription>, IComparable<TopicDescription>, 
		IComparable, PathBasedEntity,
		EntityDescription
	{
	}
}