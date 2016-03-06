# Inner workings
This section contains information about the inner workings of `RawRabbit`. It can be a useful reference guide for users who wants to extend or modify the standard behaviour of the framework.

## ChannelFactory
The default implementation of the `IChannelFactory` is aptly named `ChannelFactory`. It has two main methods

* `GetChannelAsync` returns an existing open channel that is reused by other operations in the application.
* `CreateChannelAsync` return an new, open channel that the caller is responsible to close.

### Avoiding 'Pipelining' exceptions
It is forbidden to perform multiple synchronous operations on the same channel. Note that synchronous and asynchronous in this section does not refer to Microsoft's `Task` execution, but rahter how the call is handled by the broker. Synchronous operations include declaring queues and exchanges. It is not adviced to use `GetChannelAsync` and perform a synchronious operation, as you may get a `Pipelining of requests forbidden` exception.

### Managing channel count
The `ChannelFactory` is configured with the `ChannelFactoryConfiguration` object. The default behaviour is to re-use the same open channel whenever `GetChannelAsync` is called. `MaxChannelCount` states the maximum amout of channels in the channel pool.

#### Initialize multiple channels
The property `InitialChannelCount` can be used to define the number of channels that will be initialied as the `ChannelFactory` is initialzed.

#### Dynamic scaling of channel count
It is possible to open and close aditional channels if the workload for the currently open channels are above the specified threshold `WorkThreshold`. Note that `EnableScaleUp` and/or `EnableScaleDown` needs to be set to `true` to have scaling enabled. `ScaleInterval` defines the interval for checking if scaling should be performed. If scaling down is enable, the `GracefulCloseInterval` is used to know how long to wait before closing the channel. It is recommended to let the graceful close interval be a couple of minutes to make sure that the channel is not in used in other classes.

### Alternative implementations
The `ThreadBasedChannelFactory` uses a `ThreadLocal<IModel>` property to make sure that channels are only used in one thread. 

## ConsumerFactory
It is the consumer factory's responsibility to wire up and return an `IRawConsumer`. The `IRawConsumer` has to implementations, `EventingRawConsumer` (default) that inherits from `EventingBasicConsumer` and `QueueingRawConsumer` that inherits from `QueueingBasicConsumer`.

## TopologyProvider
The `TopologyProvider` has async methods for creating topology features, such as queues and exchanges. In order to prevent pipelinging exception, it uses it's own private channel that is disposes two seconds after last usage. It keeps a list of queues and exchanges that is has declared, so that if a `DeclareQueueAsync` is called for a queue recently declared, it returns without doing a roundtrip to the broker.