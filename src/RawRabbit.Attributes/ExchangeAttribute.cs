using System;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExchangeAttribute : Attribute
    {
        internal bool? NullableDurability;
        internal bool? NullableAutoDelete;

        public string Name { get; set; }
        public ExchangeType Type { get; set; }
        public bool Durable { set { NullableDurability = value; } }
        public bool AutoDelete { set { NullableAutoDelete = value; } }
    }
}
