﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;

namespace Silverback.Messaging.Messages
{
    public static class HeadersExtensions
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        public static Confluent.Kafka.Header ToConfluentHeader(this MessageHeader header) =>
            new Confluent.Kafka.Header(header.Key, Encoding.GetBytes(header.Value));

        public static MessageHeader ToSilverbackHeader(this Confluent.Kafka.IHeader header) =>
            new MessageHeader(header.Key, Encoding.GetString(header.GetValueBytes()));
    }
}
