---
title: Outbound Connector
permalink: /docs/configuration/outbound
---

The outbound connector is used to automatically relay the integration messages (published to the internal bus) to the message broker. Multiple outbound endpoints can be configured and Silverback will route the messages according to their type (based on the `TMessage` parameter passed to the `AddOutbound<TMessage>` method.

## Implementations

Multiple implementations of the connector are available, offering a variable degree of reliability.

### Basic

The basic `OutboundConnector` is very simple and relays the messages synchronously. This is the easiest, better performing and most lightweight option but it doesn't allow for any transactionality (once the message is fired, is fired) nor resiliency to the message broker failure.

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()
        .AddBroker<KafkaBroker>(options => options
            .AddOutboundConnector());
    ...
}
public void Configure(BusConfigurator busConfigurator)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("basket-events")
                {
                    ...
                }));
```

### Deferred

The `DeferredOutboundConnector` will store the outbound messages into a database table and produce them asynchronously. This allows to take advantage of database transactions, preventing inconsistencies. And in addition allows the system to retry indefinitely if the message broker is not available.

The **Silverback.Integration.EntityFrameworkCore** package contains an implementation that allows to store the outbound messages into a DbSet, being therefore implicitly saved in the same transaction used to save changes to the local data.

The `DbContext` must include a `DbSet<OutboundMessage>` and an `OutboundWorker` is to be scheduled using your scheduler of choice to process the outbound queue.

**Important!** The current `OutboundWorker` cannot be horizontally scaled and starting multiple instances will cause the messages to be produced multiple times. In the following example a distributed lock in the database is used to ensure that only one instance is running and another one will _immediatly_ take over when it stops (the `DbContext` must include a `DbSet<Lock>` as well).
{: .notice--warning}

```c#
public void ConfigureServices(IServiceCollection services)
{
    ...

    services
        .AddBus()

        // Setup the background task manager using the database
        // to handle the distributed locks
        .AddBackgroundTaskManager<MyDbContext>()

        .AddBroker<KafkaBroker>(options => options
            .AddDbOutboundConnector<MyDbContext>()
            .AddDbOutboundWorker<MyDbContext>());
    ...
}

public void Configure(BusConfigurator busConfigurator, OutboundQueueWorker outboundWorker, IApplicationLifetime appLifetime)
{
    busConfigurator
        .Connect(endpoints => endpoints
            .AddOutbound<IIntegrationEvent>(
                new KafkaProducerEndpoint("catalog-events")
                {
                    ...
                }));

    // Start processing the outbound queue:
    // -> take appLifetime.ApplicationStopping as cancellation token
    // -> sleep 500 milliseconds when the queue is empty
    // -> check if the lock is gone every 1 seconds (= the running instance was stopped and we need to take over)
    outboundWorker.StartProcessing(
        appLifetime.ApplicationStopping,
        TimeSpan.FromMilliseconds(500),
        new Background.DistributedLockSettings(
            acquireRetryInterval: TimeSpan.FromSeconds(1))
        );
        jobScheduler.AddJob("outbound-queue-worker", TimeSpan.FromMilliseconds(100),
        s => s.GetRequiredService<OutboundQueueWorker>().ProcessQueue());
}
```

#### Extensibility

You can easily create another implementation targeting another kind of storage, simply creating your own `IOutboundQueueProducer` and `IOutboundQueueConsumer`.
It is then suggested to create an extension method for the `BrokerOptionsBuilder` to register your own types.

```c#
public static BrokerOptionsBuilder AddMyCustomOutboundConnector(this BrokerOptionsBuilder builder)
{
    builder.AddOutboundConnector<DeferredOutboundConnector>();
    builder.Services.AddScoped<IOutboundQueueProducer, MyCustomQueueProducer>();

    return builder;
}

public static BrokerOptionsBuilder AddMyCustomOutboundWorker<TDbContext>(this BrokerOptionsBuilder builder,
    bool enforceMessageOrder = true, int readPackageSize = 100)
{
    builder.AddOutboundWorker(enforceMessageOrder, readPackageSize);
    builder.Services.AddScoped<IOutboundQueueConsumer, MyCustomQueueConsumer>();

    return builder;
}
```
