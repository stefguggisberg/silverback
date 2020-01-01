﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.EntityFrameworkCore
{
    /// <summary>
    ///     Exposes some methods to handle domain events as part of the SaveChanges transaction.
    /// </summary>
    public class DbContextEventsPublisher
    {
        private readonly Func<object, IEnumerable<object>> _eventsSelector;
        private readonly Action<object> _clearEventsAction;
        private readonly IPublisher _publisher;
        private readonly DbContext _dbContext;

        public DbContextEventsPublisher(IPublisher publisher, DbContext dbContext)
            : this(
                e => (e as IMessagesSource)?.GetMessages(),
                e => (e as IMessagesSource)?.ClearMessages(),
                publisher, dbContext)
        {
        }

        public DbContextEventsPublisher(
            Func<object, IEnumerable<object>> eventsSelector,
            Action<object> clearEventsAction,
            IPublisher publisher,
            DbContext dbContext)
        {
            _eventsSelector = eventsSelector ?? throw new ArgumentNullException(nameof(eventsSelector));
            _clearEventsAction = clearEventsAction ?? throw new ArgumentNullException(nameof(clearEventsAction));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        ///     Publishes the domain events generated by the tracked entities and then executes the provided save changes
        ///     procedure.
        /// </summary>
        public int ExecuteSaveTransaction(Func<int> saveChanges) =>
            ExecuteSaveTransaction(() => Task.FromResult(saveChanges()), false).Result;

        /// <summary>
        ///     Publishes the domain events generated by the tracked entities and then executes the provided save changes
        ///     procedure.
        /// </summary>
        public Task<int> ExecuteSaveTransactionAsync(Func<Task<int>> saveChangesAsync) =>
            ExecuteSaveTransaction(saveChangesAsync, true);

        private async Task<int> ExecuteSaveTransaction(Func<Task<int>> saveChanges, bool async)
        {
            await PublishEvent<TransactionStartedEvent>(async);

            var saved = false;
            try
            {
                await PublishDomainEvents(async);

                var result = await saveChanges();

                saved = true;

                await PublishEvent<TransactionCompletedEvent>(async);

                return result;
            }
            catch (Exception)
            {
                if (!saved)
                    await PublishEvent<TransactionAbortedEvent>(async);

                throw;
            }
        }

        private async Task PublishDomainEvents(bool async)
        {
            var events = GetDomainEvents();

            // Keep publishing events fired inside the event handlers
            while (events.Any())
            {
                if (async)
                    await _publisher.PublishAsync(events);
                else
                    _publisher.Publish(events);

                events = GetDomainEvents();
            }
        }

        private IReadOnlyCollection<object> GetDomainEvents() =>
            _dbContext
                .ChangeTracker.Entries()
                .SelectMany(e =>
                {
                    var selected = _eventsSelector(e.Entity)?.ToList();

                    // Clear all events to avoid firing the same event multiple times during the recursion
                    _clearEventsAction(e.Entity);

                    return selected ?? Enumerable.Empty<object>();
                }).ToList();

        private async Task PublishEvent<TEvent>(bool async)
            where TEvent : new()
        {
            if (async)
                await _publisher.PublishAsync(new TEvent());
            else
                _publisher.Publish(new TEvent());
        }
    }
}