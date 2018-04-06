﻿using NUnit.Framework;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes;

namespace Silverback.Tests.Messaging.Broker
{
    [TestFixture]
    public class BrokerTest
    {
        [Test]
        public void GetDefaultSerializerTest()
        {
            var serializer = new FakeBroker().GetSerializer();

            Assert.That(serializer, Is.InstanceOf<JsonMessageSerializer>());
        }

        [Test]
        public void GetSerializerTest()
        {
            var broker = new FakeBroker();
            broker.SerializeUsing<FakeSerializer>();
            var serializer = broker.GetSerializer();

            Assert.That(serializer, Is.InstanceOf<FakeSerializer>());
        }

        [Test]
        public void GetCachedSerializerTest()
        {
            var broker = new FakeBroker();
            broker.SerializeUsing<FakeSerializer>();
            var serializer = broker.GetSerializer();
            var serializer2 = broker.GetSerializer();
            Assert.That(serializer, Is.SameAs(serializer2));
        }

        [Test]
        public void WithNameTest()
        {
            var broker = new FakeBroker().WithName("TestBroker");

            Assert.That(broker.Name, Is.EqualTo("TestBroker"));
        }

        [Test]
        public void AsDefaultTest()
        {
            var broker = new FakeBroker();
            Assert.That(broker.IsDefault, Is.False);

            broker.AsDefault();

            Assert.That(broker.IsDefault, Is.True);
        }
    }
}
