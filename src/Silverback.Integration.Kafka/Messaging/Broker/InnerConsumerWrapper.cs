﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    internal class InnerConsumerWrapper : IDisposable
    {
        private readonly List<KafkaConsumerEndpoint> _endpoints = new List<KafkaConsumerEndpoint>();
        private readonly ConsumerConfig _config;
        private readonly bool _enableAutoRecovery;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly TimeSpan _recoveryDelay = TimeSpan.FromSeconds(5);

        private IConsumer<byte[], byte[]> _innerConsumer;

        private bool _consuming;
        private bool _consumedAtLeastOnce;

        public InnerConsumerWrapper(
            ConsumerConfig config,
            bool enableAutoRecovery,
            IServiceProvider serviceProvider,
            ILogger logger)
        {
            _config = config;
            _enableAutoRecovery = enableAutoRecovery;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public event KafkaMessageReceivedHandler Received;

        public void Subscribe(KafkaConsumerEndpoint endpoint)
        {
            _endpoints.Add(endpoint);
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets) =>
            _innerConsumer.Commit(offsets);

        public void StoreOffset(IEnumerable<TopicPartitionOffset> offsets) =>
            offsets.ForEach(_innerConsumer.StoreOffset);

        public void CommitAll()
        {
            if (!_consumedAtLeastOnce) return;

            try
            {
                var offsets = _innerConsumer.Commit();
                CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(
                    offsets.Select(offset =>
                            new TopicPartitionOffsetError(
                                offset,
                                new Error(ErrorCode.NoError)))
                        .ToList()));
            }
            catch (TopicPartitionOffsetException ex)
            {
                CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(ex.Results, ex.Error));
                throw;
            }
            catch (KafkaException ex)
            {
                CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(null, ex.Error));
            }
        }

        public void Seek(TopicPartitionOffset tpo) => _innerConsumer.Seek(tpo);

        public void StartConsuming()
        {
            if (_consuming)
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            InitInnerConsumer();

            Task.Run(Consume);
        }

        public void StopConsuming()
        {
            if (!_consuming)
                return;

            _cancellationTokenSource.Cancel();

            // Wait until stopped for real before returning to avoid
            // exceptions when the process exits prematurely
            while (_consuming) Thread.Sleep(100);

            _cancellationTokenSource.Dispose();
        }

        private void InitInnerConsumer()
        {
            _innerConsumer = BuildConfluentConsumer();
            Subscribe();
        }

        private void Subscribe() => _innerConsumer.Subscribe(_endpoints.SelectMany(e => e.Names));

        private IConsumer<byte[], byte[]> BuildConfluentConsumer() =>
            new ConsumerBuilder<byte[], byte[]>(_config)
                .SetPartitionsAssignedHandler((_, partitions) =>
                {
                    partitions.ForEach(partition =>
                    {
                        _logger.LogInformation("Assigned topic {topic} partition {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, _innerConsumer.MemberId);
                    });

                    CreateScopeAndPublishEvent(
                        new KafkaPartitionsAssignedEvent(partitions, _innerConsumer.MemberId));
                })
                .SetPartitionsRevokedHandler((_, partitions) =>
                {
                    partitions.ForEach(partition =>
                    {
                        _logger.LogInformation("Revoked topic {topic} partition {partition}, member id: {memberId}",
                            partition.Topic, partition.Partition, _innerConsumer.MemberId);
                    });

                    CreateScopeAndPublishEvent(
                        new KafkaPartitionsRevokedEvent(partitions, _innerConsumer.MemberId));
                })
                .SetOffsetsCommittedHandler((_, offsets) =>
                {
                    foreach (var offset in offsets.Offsets)
                    {
                        if (offset.Offset == Offset.Unset)
                            continue;

                        if (offset.Error != null && offset.Error.Code != ErrorCode.NoError)
                        {
                            _logger.LogError(
                                "Error occurred committing the offset {topic} {partition} @{offset}: {errorCode} - {errorReason} ",
                                offset.Topic, offset.Partition, offset.Offset, offset.Error.Code, offset.Error.Reason);
                        }
                        else
                        {
                            _logger.LogTrace("Successfully committed offset {topic} {partition} @{offset}.",
                                offset.Topic, offset.Partition, offset.Offset);
                        }

                        CreateScopeAndPublishEvent(new KafkaOffsetsCommittedEvent(offsets));
                    }
                })
                .SetErrorHandler((_, error) =>
                {
                    // Ignore errors if not consuming anymore
                    // (lidrdkafka randomly throws some "brokers are down"
                    // while disconnecting)
                    if (!_consuming) return;

                    _logger.Log(error.IsFatal ? LogLevel.Critical : LogLevel.Error,
                        "Error in Kafka consumer: {reason}.", error.Reason);

                    CreateScopeAndPublishEvent(new KafkaErrorEvent(error));
                })
                .SetStatisticsHandler((_, json) =>
                {
                    _logger.LogInformation($"Statistics: {json}");

                    CreateScopeAndPublishEvent(new KafkaStatisticsEvent(json));
                })
                .Build();

        private async Task Consume()
        {
            _consuming = true;

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await ReceiveMessage();
                }
                catch (OperationCanceledException)
                {
                    _logger.LogTrace("Consuming canceled.");
                }
                catch (KafkaException ex)
                {
                    if (!AutoRecoveryIfEnabled(ex))
                        break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Fatal error occurred consuming a message. The consumer will be stopped.");
                    break;
                }
            }

            _consuming = false;
        }

        private async Task ReceiveMessage()
        {
            var result = _innerConsumer.Consume(_cancellationTokenSource.Token);

            if (result == null)
                return;

            if (result.IsPartitionEOF)
            {
                _logger.LogInformation("Partition EOF reached: {topic} {partition} @{offset}", result.Topic,
                    result.Partition,
                    result.Offset);
                return;
            }

            _consumedAtLeastOnce = true;
            _logger.LogTrace("Consuming message: {topic} {partition} @{offset}", result.Topic, result.Partition,
                result.Offset);

            if (Received != null)
                await Received.Invoke(result.Message, result.TopicPartitionOffset);
        }

        private bool AutoRecoveryIfEnabled(KafkaException ex)
        {
            if (_enableAutoRecovery)
            {
                _logger.LogWarning(ex, "KafkaException occurred. The consumer will try to recover.");
                ResetInnerConsumer();
            }
            else
            {
                _logger.LogCritical(ex, "Fatal error occurred consuming a message. The consumer will be stopped. " +
                                        "Enable auto recovery to allow Silverback to automatically try to reconnect " +
                                        "(EnableAutoRecovery=true in the endpoint configuration).");
            }

            return _enableAutoRecovery;
        }

        private void ResetInnerConsumer()
        {
            while (true)
            {
                try
                {
                    DisposeInnerConsumer();
                    InitInnerConsumer();

                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex,
                        "Failed to recover from consumer exception. " +
                        $"Will retry in {_recoveryDelay.TotalSeconds} seconds.");

                    Thread.Sleep(_recoveryDelay);
                }
            }
        }

        private void DisposeInnerConsumer()
        {
            _innerConsumer?.Close();
            _innerConsumer?.Dispose();
            _innerConsumer = null;
        }

        private void CreateScopeAndPublishEvent(IMessage message)
        {
            using var scope = _serviceProvider?.CreateScope();
            var publisher = scope?.ServiceProvider.GetRequiredService<IPublisher>();

            publisher?.Publish(message);
        }

        public void Dispose()
        {
            StopConsuming();
            DisposeInnerConsumer();
            _cancellationTokenSource?.Dispose();
        }
    }
}