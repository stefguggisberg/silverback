﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging;
using Silverback.Messaging.LargeMessages;

namespace Silverback.Tests.Integration.TestTypes
{
    public sealed class TestProducerEndpoint : TestEndpoint, IProducerEndpoint, IEquatable<TestProducerEndpoint>
    {
        public TestProducerEndpoint(string name) : base(name)
        {
        }

        public ChunkSettings Chunk { get; set; } = new ChunkSettings();
        
        #region IEquatable

        public bool Equals(TestProducerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is TestProducerEndpoint endpoint && Equals(endpoint);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        #endregion
    }
}