using System;

namespace RawRabbit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class QueueAttribute : Attribute
    {
        internal bool? NullableDurability;
        internal bool? NullableExclusitivy;
        internal bool? NullableAutoDelete;

        public string Name { get; set; }

        public bool Durable
        {
            get { return NullableDurability.GetValueOrDefault(); }
            set { NullableDurability = value; }
        }

        public bool Exclusive
        {
            get { return NullableExclusitivy.GetValueOrDefault(); }
            set { NullableExclusitivy = value; }
        }

        public bool AutoDelete
        {
            get { return NullableAutoDelete.GetValueOrDefault(); }
            set { NullableAutoDelete = value; }
        }
        public int MessageTtl { get; set; }
        public byte MaxPriority { get; set; }
        public string DeadLeterExchange { get; set; }
        public string Mode { get; set; }
    }
}
