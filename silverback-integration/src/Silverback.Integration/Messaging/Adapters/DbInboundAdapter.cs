﻿using System;
using System.Transactions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Repositories;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that subscribes to the message broker and forwards the messages to the internal bus.<br/>
    /// This implementation uses an inbox table to prevent duplicated processing of the same message. 
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Adapters.IInboundAdapter" />
    public class DbInboundAdapter<TEntity> : SimpleInboundAdapter
        where TEntity : IInboundMessageEntity
    {
        private readonly IInboundMessagesRepository<TEntity> _inboxRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbInboundAdapter{TEntity}"/> class.
        /// </summary>
        /// <param name="inboxRepository">The inbox repository.</param>
        /// <exception cref="ArgumentNullException">inboxRepository</exception>
        public DbInboundAdapter(IInboundMessagesRepository<TEntity> inboxRepository)
        {
            _inboxRepository = inboxRepository ?? throw new ArgumentNullException(nameof(inboxRepository));
        }

        /// <summary>
        /// Relays the message ensuring that it wasn't processed already by this microservice.
        /// </summary>
        /// <param name="message">The message.</param>
        protected override void RelayMessage(IIntegrationMessage message)
        {
            if (_inboxRepository.Exists(message.Id))
            {
                // TODO: Trace
                return;
            }

            var entity = _inboxRepository.Create();
            entity.MessageId = message.Id;
            entity.Received = DateTime.UtcNow;
            _inboxRepository.Add(entity);

            base.RelayMessage(message);

            // Call save changes, in case the changes weren't committed
            // already as part of the message handling transaction.
            _inboxRepository.SaveChanges();
        }
    }
}