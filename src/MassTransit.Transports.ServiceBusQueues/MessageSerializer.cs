using System.IO;
using MassTransit.Serialization;

namespace MassTransit.Transports.ServiceBusQueues
{
	public class MessageSerializer
		: IMessageSerializer
	{
		void IMessageSerializer.Serialize<T>(Stream stream, ISendContext<T> context)
		{
			throw new System.NotImplementedException();
		}

		void IMessageSerializer.Deserialize(IReceiveContext context)
		{
			throw new System.NotImplementedException();
		}

		string IMessageSerializer.ContentType
		{
			get { throw new System.NotImplementedException(); }
		}
	}
}