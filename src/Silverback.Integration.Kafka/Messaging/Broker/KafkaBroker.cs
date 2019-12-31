﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for Apache Kafka.
    /// </summary>
    public class KafkaBroker : Broker
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MessageLogger _messageLogger;

        public KafkaBroker(
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            MessageLogger messageLogger)
            : base(behaviors, loggerFactory)
        {
            _messageKeyProvider = messageKeyProvider;
            _loggerFactory = loggerFactory;
            _messageLogger = messageLogger;
        }

        /// <inheritdoc cref="Broker" />
        protected override IProducer InstantiateProducer(
            IProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors) =>
            new KafkaProducer(
                this,
                (KafkaProducerEndpoint) endpoint,
                _messageKeyProvider,
                behaviors,
                _loggerFactory.CreateLogger<KafkaProducer>(),
                _messageLogger);

        /// <inheritdoc cref="Broker" />
        protected override IConsumer InstantiateConsumer(
            IConsumerEndpoint endpoint,
            IEnumerable<IConsumerBehavior> behaviors) =>
            new KafkaConsumer(
                this,
                (KafkaConsumerEndpoint) endpoint,
                behaviors,
                _loggerFactory.CreateLogger<KafkaConsumer>());
    }
}