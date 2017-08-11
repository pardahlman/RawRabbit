using System;

namespace RawRabbit.Enrichers.Attributes
{
	[AttributeUsage(AttributeTargets.Class)]
	public class RoutingAttribute : Attribute
	{
		internal bool? NullableAutoAck;

		public string RoutingKey { get; set; }
		public ushort PrefetchCount { get; set; }
		[Obsolete("Property name changed. Use 'WithAutoAck' instead.")]
		public bool NoAck { get { return NullableAutoAck.GetValueOrDefault(); } set { NullableAutoAck = value; } }
		public bool AutoAck { get { return NullableAutoAck.GetValueOrDefault(); } set { NullableAutoAck = value; } }
	}
}
