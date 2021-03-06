﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public interface IOutboundMessage : IBrokerMessage
    {
    }

    public interface IOutboundMessage<out TContent> : IOutboundMessage
    {
        /// <summary>
        /// Gets the deserialized message body.
        /// </summary>
        new TContent Content { get; }
    }
}