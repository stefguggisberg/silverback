// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Broker : IBroker, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IProducerBehavior> _producerBehaviors;
        private readonly IEnumerable<IConsumerBehavior> _consumerBehaviors;

        private ConcurrentDictionary<IEndpoint, Producer> _producers = new ConcurrentDictionary<IEndpoint, Producer>();
        private List<Consumer> _consumers = new List<Consumer>();

        protected readonly ILoggerFactory LoggerFactory;

        protected Broker(IEnumerable<IBrokerBehavior> behaviors, ILoggerFactory loggerFactory)
        {
            _producerBehaviors = behaviors?.OfType<IProducerBehavior>() ?? Enumerable.Empty<IProducerBehavior>();
            _consumerBehaviors = behaviors?.OfType<IConsumerBehavior>() ?? Enumerable.Empty<IConsumerBehavior>();

            LoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        #region Producer / Consumer

        public virtual IProducer GetProducer(IEndpoint endpoint)
        {
            return _producers.GetOrAdd(endpoint, _ =>
            {
                _logger.LogInformation("Creating new producer for endpoint {endpointName}. " +
                                       $"(Total producers: {_producers.Count + 1})", endpoint.Name);
                return InstantiateProducer(endpoint, _producerBehaviors);
            });
        }

        protected abstract Producer InstantiateProducer(IEndpoint endpoint, IEnumerable<IProducerBehavior> behaviors);

        public virtual IConsumer GetConsumer(IEndpoint endpoint)
        {
            if (IsConnected)
                throw new InvalidOperationException("The broker is already connected. Disconnect it to get a new consumer.");

            _logger.LogInformation("Creating new consumer for endpoint {endpointName}.", endpoint.Name);

            var consumer = InstantiateConsumer(endpoint, _consumerBehaviors);

            lock (_consumers)
            {
                _consumers.Add(consumer);
            }

            return consumer;
        }

        protected abstract Consumer InstantiateConsumer(IEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors);

        #endregion

        #region Connect / Disconnect

        public bool IsConnected { get; private set; }

        public void Connect()
        {
            if (IsConnected)
                return;

            _logger.LogTrace("Connecting to message broker...");

            Connect(_consumers);
            IsConnected = true;

            _logger.LogTrace("Connected to message broker!");
        }

        protected abstract void Connect(IEnumerable<IConsumer> consumers);

        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _logger.LogTrace("Disconnecting from message broker...");

            Disconnect(_consumers);
            IsConnected = false;

            _logger.LogTrace("Disconnected from message broker!");
        }

        protected abstract void Disconnect(IEnumerable<IConsumer> consumers);

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Disconnect();

            _consumers?.OfType<IDisposable>().ForEach(o => o.Dispose());
            _consumers = null;

            _producers?.Values.OfType<IDisposable>().ForEach(o => o.Dispose());
            _producers = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    public abstract class Broker<TEndpoint> : Broker
        where TEndpoint : class, IEndpoint
    {
        protected Broker(IEnumerable<IBrokerBehavior> behaviors, ILoggerFactory loggerFactory)
            : base(behaviors, loggerFactory)
        {
        }

        public override IProducer GetProducer(IEndpoint endpoint)
        {
            ThrowIfWrongEndpointType(endpoint);

            return base.GetProducer(endpoint);
        }

        public override IConsumer GetConsumer(IEndpoint endpoint)
        {
            ThrowIfWrongEndpointType(endpoint);

            return base.GetConsumer(endpoint);
        }

        public static void ThrowIfWrongEndpointType(IEndpoint endpoint)
        {
            if (!(endpoint is TEndpoint))
                throw new ArgumentException($"An endpoint of type {typeof(TEndpoint).Name} is expected.");
        }
    }
}