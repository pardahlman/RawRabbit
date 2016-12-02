using System.Collections.Generic;
using System.Linq;

namespace RawRabbit.Operations.Saga.Model
{
	public class TriggerBuilder<TTrigger>
	{
		private readonly List<TriggerInvokerAppender> _triggerAppenders;

		public TriggerBuilder()
		{
			_triggerAppenders = new List<TriggerInvokerAppender>();
		}
		public TriggerInvokerAppender Configure(TTrigger trigger)
		{
			var appender = new TriggerInvokerAppender(trigger);
			_triggerAppenders.Add(appender);
			return appender;
		}

		public List<TriggerInvoker> Build()
		{
			return _triggerAppenders
				.SelectMany(t => t.TriggerInvokers)
				.ToList();
		}
	}
}