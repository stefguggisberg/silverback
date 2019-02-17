﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    public class Publisher : IPublisher
    {
        private readonly ILogger _logger;

        private readonly BusOptions _options;
        private readonly IEnumerable<IBehavior> _behaviors;
        private readonly IServiceProvider _serviceProvider;

        private IEnumerable<SubscribedMethod> _subscribedMethods;
        private SubscribedMethodInvoker _methodInvoker;

        public Publisher(BusOptions options, IServiceProvider serviceProvider, ILogger<Publisher> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _logger = logger;
            _behaviors = serviceProvider.GetServices<IBehavior>();
        }

        public void Publish(object message) => 
            Publish(new[] { message });

        public Task PublishAsync(object message) => 
            PublishAsync(new[] { message });

        public IEnumerable<TResult> Publish<TResult>(object message) => 
            Publish<TResult>(new[] { message });

        public Task<IEnumerable<TResult>> PublishAsync<TResult>(object message) => 
            PublishAsync<TResult>(new[] { message });

        public void Publish(IEnumerable<object> messages) => 
            Publish(messages, false).Wait();

        public Task PublishAsync(IEnumerable<object> messages) => 
            Publish(messages, true);

        public IEnumerable<TResult> Publish<TResult>(IEnumerable<object> messages) => 
            Publish(messages, false).Result.Cast<TResult>().ToList();

        public async Task<IEnumerable<TResult>> PublishAsync<TResult>(IEnumerable<object> messages) => 
            (await Publish(messages, true)).Cast<TResult>().ToList();

        // TODO: Test recursion
        private async Task<IEnumerable<object>> Publish(IEnumerable<object> messages, bool executeAsync)
        {
            var messagesList = messages?.ToList();

            if (messagesList == null || !messagesList.Any())
                return Enumerable.Empty<object>();

            return await ExecutePipeline(_behaviors, messagesList, async m =>
                (await InvokeExclusiveMethods(m, executeAsync))
                .Union(await InvokeNonExclusiveMethods(m, executeAsync))
                .ToList());
        }

        private Task<IEnumerable<object>> ExecutePipeline(IEnumerable<IBehavior> behaviors, IEnumerable<object> messages, Func<IEnumerable<object>, Task<IEnumerable<object>>> finalAction)
        {
            if (behaviors == null || !behaviors.Any())
                return finalAction(messages);

            return behaviors.First().Handle(messages, m => ExecutePipeline(behaviors.Skip(1), m, finalAction));
        }

        private Task<IEnumerable<object>> InvokeExclusiveMethods(IEnumerable<object> messages, bool executeAsync) =>
            GetSubscribedMethods()
                .Where(method => method.Info.IsExclusive)
                .SelectManyAsync(method => GetMethodInvoker().Invoke(method, messages, executeAsync));

        private Task<IEnumerable<object>> InvokeNonExclusiveMethods(IEnumerable<object> messagesList, bool executeAsync) =>
            GetSubscribedMethods()
                .Where(method => !method.Info.IsExclusive)
                .ParallelSelectManyAsync(method => GetMethodInvoker().Invoke(method, messagesList, executeAsync));

        private IEnumerable<SubscribedMethod> GetSubscribedMethods() =>
            _subscribedMethods ?? (_subscribedMethods = _options
                .Subscriptions
                .SelectMany(s => s.GetSubscribedMethods(_serviceProvider))
                .ToList());

        private SubscribedMethodInvoker GetMethodInvoker() =>
            _methodInvoker ?? (_methodInvoker = _serviceProvider.GetRequiredService<SubscribedMethodInvoker>());
    }
}
