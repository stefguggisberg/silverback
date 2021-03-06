﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBroker : Broker
    {

        public TestBroker(IEnumerable<IBrokerBehavior> behaviors = null)
            : base(behaviors, NullLoggerFactory.Instance)
        {
        }

        public List<TestConsumer> Consumers { get; } = new List<TestConsumer>();

        public List<ProducedMessage> ProducedMessages { get; } = new List<ProducedMessage>();

        protected override Producer InstantiateProducer(IEndpoint endpoint, IEnumerable<IProducerBehavior> behaviors) => 
            new TestProducer(this, endpoint, behaviors);

        protected override Consumer InstantiateConsumer(IEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors)
        {
            var consumer = new TestConsumer(this, endpoint, behaviors);
            Consumers.Add(consumer);
            return consumer;
        }

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
            consumers.Cast<TestConsumer>().ToList().ForEach(c => c.IsReady = true);
        }

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
        }

        public class ProducedMessage
        {
            public ProducedMessage(byte[] message, IEnumerable<MessageHeader> headers, IEndpoint endpoint)
            {
                Message = message;

                if (headers != null)
                    Headers.AddRange(headers);

                Endpoint = endpoint;
            }

            public byte[] Message { get; }
            public MessageHeaderCollection Headers { get; } = new MessageHeaderCollection();
            public IEndpoint Endpoint { get; }
        }
    }
}
