using System;
using Microsoft.ServiceBus.Messaging;

namespace MassTransit.Transports.AzureServiceBus
{
	class MessageSenderImpl : MessageSender
	{
		readonly Microsoft.ServiceBus.Messaging.TopicClient _sender;
		bool _disposed;
		readonly QueueClient _queueClient;

		public MessageSenderImpl(QueueClient queueClient)
		{
			_queueClient = queueClient;
		}

		public MessageSenderImpl(Microsoft.ServiceBus.Messaging.TopicClient sender)
		{
			_sender = sender;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool managed)
		{
			if (!managed || _disposed) 
				return;

			try
			{
				if (_queueClient != null && !_queueClient.IsClosed)
					_queueClient.Close();

				if (_sender != null && !_sender.IsClosed)
					_sender.Close();
			}
			finally
			{
				_disposed = true;
			}
		}

		public IAsyncResult BeginSend(BrokeredMessage message, AsyncCallback callback, object state)
		{
			return _queueClient != null
			       	? _queueClient.BeginSend(message, callback, state)
			       	: _sender.BeginSend(message, callback, state);
		}

		public void EndSend(IAsyncResult result)
		{
			if (_queueClient != null)
				_queueClient.EndSend(result);
			else
				_sender.EndSend(result);
		}
	}
}