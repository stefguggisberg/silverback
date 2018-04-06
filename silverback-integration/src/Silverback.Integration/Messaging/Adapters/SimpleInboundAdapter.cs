﻿using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.<br/>
    /// This is the simplest implementation and it doesn't prevent duplicated processing of the same message. 
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Adapters.IInboundAdapter" />
    public class SimpleInboundAdapter : IInboundAdapter, IDisposable
    {
        private IBus _bus;
        private IConsumer _consumer;
        private IMessageSerializer _serializer;

        /// <summary>
        /// Initializes the <see cref="T:Silverback.Messaging.Adapters.IInboundAdapter" />.
        /// </summary>
        /// <param name="bus">The internal <see cref="IBus" /> where the messages have to be relayed.</param>
        /// <param name="endpoint">The endpoint this adapter has to connect to.</param>
        public void Init(IBus bus, IEndpoint endpoint)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            endpoint.ValidateConfiguration();

            Connect(endpoint);
        }

        /// <summary>
        /// Implements the logic to connect and start listening to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        protected virtual void Connect(IEndpoint endpoint)
        {
            if (_consumer != null)
                throw new InvalidOperationException("Connect was called twice.");

            _consumer = endpoint.GetConsumer();
            
            // TODO: Handle errors -> logging and stuff -> then?
            _consumer.Consume(e => RelayMessage(e.Message));
        }

        /// <summary>
        /// Relays the message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void RelayMessage(IIntegrationMessage message)
        {
            _bus.Publish(message);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                (_consumer as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SimpleInboundAdapter"/> class.
        /// </summary>
        ~SimpleInboundAdapter()
        {
            Dispose(false);
        }
    }
}