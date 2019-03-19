﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Core.Model.Tests.TestTypes.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Model.Tests.TestTypes.Subscribers
{
    public class QueriesHandler : ISubscriber
    {
        [Subscribe]
        public Task<IEnumerable<int>> Handle(ListQuery query) => Task.FromResult(Enumerable.Range(1, query.Count));
    }
}
