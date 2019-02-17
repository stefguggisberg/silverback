﻿using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Publishing;

namespace Silverback.Core.Tests.TestTypes.Behaviors
{
    public class TestBehavior : IBehavior
    {
        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public Task<IEnumerable<object>> Handle(IEnumerable<object> messages, MessagesHandler next)
        {
            EnterCount++;

            var result = next(messages);

            ExitCount++;

            return result;
        }
    }
}
