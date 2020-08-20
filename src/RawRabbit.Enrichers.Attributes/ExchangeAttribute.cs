using System;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Enrichers.Attributes
{
	[AttributeUsage(AttributeTargets.Class)]
	public class ExchangeAttribute : Attribute
	{
		internal bool? NullableDurability;
		internal bool? NullableAutoDelete;

		public string Name { get; set; }
		public ExchangeType Type { get; set; }
		public bool Durable { get { return NullableDurability.GetValueOrDefault(); } set { NullableDurability = value; } }
		public bool AutoDelete { get { return NullableAutoDelete.GetValueOrDefault(); } set { NullableAutoDelete = value; } }
	}
}
