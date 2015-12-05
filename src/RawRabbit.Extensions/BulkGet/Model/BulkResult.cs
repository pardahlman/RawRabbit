using System;
using System.Collections.Generic;
using System.Linq;
using RawRabbit.Context;

namespace RawRabbit.Extensions.BulkGet.Model
{
	public class BulkResult<TMessageContext> : IDisposable where TMessageContext : IMessageContext
	{
		private readonly IDictionary<Type, List<IBulkMessage>> _messageTypeToMessages;

		public BulkResult(IDictionary<Type, List<IBulkMessage>> messageTypeToMessages)
		{
			_messageTypeToMessages = messageTypeToMessages;
		}

		public void AckAll()
		{
			foreach (var bulkMessage in GetAllMessages())
			{
				bulkMessage.Ack();
			}
		}

		public void NackAll(bool requeue = true)
		{
			foreach (var bulkMessage in GetAllMessages())
			{
				bulkMessage.Nack(requeue);
			}
		}

		private IEnumerable<IBulkMessage> GetAllMessages()
		{
			return _messageTypeToMessages.Values
				.Where(msgs => msgs != null)
				.SelectMany(msgs => msgs);
		}

		public IEnumerable<BulkMessage<TMessage, TMessageContext>> GetMessages<TMessage>()
		{
			List<IBulkMessage> messages;
			return _messageTypeToMessages.TryGetValue(typeof (TMessage), out messages)
				? messages.OfType<BulkMessage<TMessage, TMessageContext>>()
				: Enumerable.Empty<BulkMessage<TMessage, TMessageContext>>();
		}

		public void Dispose()
		{
			foreach (var disposable in GetAllMessages().OfType<IDisposable>())
			{
				disposable?.Dispose();
			}
		}
	}
}
