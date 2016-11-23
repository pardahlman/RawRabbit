using System;

namespace RawRabbit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RoutingAttribute : Attribute
    {
        internal bool? NullableNoAck;

        public string RoutingKey { get; set; }
        public ushort PrefetchCount { get; set; }
        public bool NoAck { get { return NullableNoAck.GetValueOrDefault(); } set { NullableNoAck = value; } }
    }
}
