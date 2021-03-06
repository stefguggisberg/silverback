// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    public interface ICommandPublisher
    {
        void Execute(ICommand commandMessage);

        Task ExecuteAsync(ICommand commandMessage);

        void Execute(IEnumerable<ICommand> commandMessages);

        Task ExecuteAsync(IEnumerable<ICommand> commandMessages);

        TResult Execute<TResult>(ICommand<TResult> commandMessage);

        Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage);

        IEnumerable<TResult> Execute<TResult>(IEnumerable<ICommand<TResult>> commandMessages);

        Task<IEnumerable<TResult>> ExecuteAsync<TResult>(IEnumerable<ICommand<TResult>> commandMessages);
    }
}