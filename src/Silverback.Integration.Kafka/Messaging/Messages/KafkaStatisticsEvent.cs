﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event fired when statistics are received. Statistics are provided as a JSON formatted string
    ///     as defined here: https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md
    /// </summary>
    /// <remarks>
    ///     You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
    ///     configuration parameter (disabled by default).
    /// </remarks>
    public class KafkaStatisticsEvent : IMessage
    {
        public KafkaStatisticsEvent(string json)
        {
            Json = json;
        }

        /// <summary>
        ///     Gets the statistics JSON (see https://github.com/edenhill/librdkafka/blob/master/STATISTICS.md)
        /// </summary>
        public string Json { get; }
    }
}