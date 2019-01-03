﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class KafkaProducerConfig : Confluent.Kafka.ProducerConfig, IEquatable<KafkaProducerConfig>
    {
        #region IEquatable

        public bool Equals(KafkaProducerConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return KafkaClientConfigComparer.Compare(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KafkaConsumerConfig)obj);
        }

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}