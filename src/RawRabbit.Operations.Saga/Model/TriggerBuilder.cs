using System.Collections.Generic;
using System.Linq;

namespace RawRabbit.Operations.Saga.Model
{
	public class TriggerBuilder<TTrigger>
	{
		private readonly List<TriggerAppender> _triggerAppenders;

		public TriggerBuilder()
		{
			_triggerAppenders = new List<TriggerAppender>();
		}
		public TriggerAppender Configure(TTrigger trigger)
		{
			var appender = new TriggerAppender(trigger);
			_triggerAppenders.Add(appender);
			return appender;
		}

		public Dictionary<TTrigger, List<ExternalTrigger>> Build()
		{
			return _triggerAppenders
				.GroupBy(t => t.Trigger)
				.ToDictionary(
					trigger => (TTrigger) trigger.Key,
					trigger => trigger.SelectMany(t => t.ExternalTriggers).ToList());
		}
	}
}