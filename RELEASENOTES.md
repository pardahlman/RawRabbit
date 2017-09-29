# 1.10.4

 - [#273](https://github.com/pardahlman/RawRabbit/pull/273) - Added .Net Standard 2.0 target framework for vNext contributed by Manuel Cabral ([Manny651](https://github.com/Manny651))
 - [#269](https://github.com/pardahlman/RawRabbit/pull/269) - Some typos contributed by ([cortex93](https://github.com/cortex93))
 - [#257](https://github.com/pardahlman/RawRabbit/issues/257) - Error in BusClientFactory.CreateDefault() on net core 2.0
 - [#233](https://github.com/pardahlman/RawRabbit/pull/233) - fix typo in NamingConventions.RetryLaterExchangeConvention contributed by Bohdan Sachkovskyi ([sachokFoX](https://github.com/sachokFoX))
 - [#231](https://github.com/pardahlman/RawRabbit/pull/231) - Fix typo in spelling `Dependency` contributed by Serhii Almazov ([almazik](https://github.com/almazik))

Commits: 36ae21d98a...4b1079f965


# 1.10.3

 - [#163](https://github.com/pardahlman/RawRabbit/pull/163) - Added queue assume initialized and tests contributed by Cemre Mengu ([cemremengu](https://github.com/cemremengu))
 - [#162](https://github.com/pardahlman/RawRabbit/pull/162) - Converted tabs to 4 spaces contributed by Cemre Mengu ([cemremengu](https://github.com/cemremengu))
 - [#161](https://github.com/pardahlman/RawRabbit/pull/161) - added editorconfig for uniform editing contributed by Cemre Mengu ([cemremengu](https://github.com/cemremengu))
 - [#160](https://github.com/pardahlman/RawRabbit/issues/160) - Add"AssumeInitialized" functionality for queues
 - [#159](https://github.com/pardahlman/RawRabbit/pull/159) - Added default broker connection values for RawRabbitConfig class contributed by Cemre Mengu ([cemremengu](https://github.com/cemremengu))
 - [#150](https://github.com/pardahlman/RawRabbit/issues/150) - StackOverflowException occures when subscribeMethod throws an exception using dotnet core
 - [#143](https://github.com/pardahlman/RawRabbit/issues/143) - Failure Recovery Issue with PublishAsync
 - [#142](https://github.com/pardahlman/RawRabbit/issues/142) - Failure Recovery
 - [#140](https://github.com/pardahlman/RawRabbit/pull/140) - (#129) Expose Mandatory Option For Publish contributed by Richard Tasker ([ritasker](https://github.com/ritasker))
 - [#136](https://github.com/pardahlman/RawRabbit/issues/136) - Newtonsoft.Json.JsonSerializationException: Error getting value from 'ScopeId' on 'System.Net.IPAddress'.
 - [#132](https://github.com/pardahlman/RawRabbit/issues/132) - Default connection timeout
 - [#129](https://github.com/pardahlman/RawRabbit/issues/129) - Expose mandatory option for publish
 - [#116](https://github.com/pardahlman/RawRabbit/issues/116) - Unable to publish message to default error exchange.

Commits: 4b6e57e351...52573f4164


# 1.10.2

 - [#117](https://github.com/pardahlman/RawRabbit/issues/117) - Propegate Topology Exception for Consumers
 - [#112](https://github.com/pardahlman/RawRabbit/pull/112) - Update Multiple-Subscribers-for-Messages.md contributed by Robert Campbell ([jayrulez](https://github.com/jayrulez))
 - [#111](https://github.com/pardahlman/RawRabbit/issues/111) - Guard against OperationInteruptedException in TopologyProvider

Commits: af0bda979e...331e174a02


# 1.10.1

 - [#108](https://github.com/pardahlman/RawRabbit/issues/108) - Upgrade to RabbitMQ.Client 4.1.0
 - [#107](https://github.com/pardahlman/RawRabbit/issues/107) - Prevent message duplicatoin on multiple Retrys
 - [#104](https://github.com/pardahlman/RawRabbit/issues/104) - Provide Serilog Logger in Ctor
 - [#103](https://github.com/pardahlman/RawRabbit/issues/103) - Support Messages in pure Json +feature
 - [#102](https://github.com/pardahlman/RawRabbit/issues/102) - Support Exception Propagation for .NET Core +feature
 - [#101](https://github.com/pardahlman/RawRabbit/issues/101) - Only dispose active Subscriptions on ShutDown +fix
 - [#100](https://github.com/pardahlman/RawRabbit/issues/100) - Run Integration Tests in AppVeyor
 - [#99](https://github.com/pardahlman/RawRabbit/issues/99) - Upgrade to RabbitMQ.Client 4.0.X

Commits: 440910db25...9f0afa415e


# 1.10.0

 - [#104](https://github.com/pardahlman/RawRabbit/issues/104) - Provide Serilog Logger in Ctor
 - [#103](https://github.com/pardahlman/RawRabbit/issues/103) - Support Messages in pure Json
 - [#102](https://github.com/pardahlman/RawRabbit/issues/102) - Support Exception Propagation for .NET Core
 - [#101](https://github.com/pardahlman/RawRabbit/issues/101) - Only dispose active Subscriptions on ShutDown
 - [#100](https://github.com/pardahlman/RawRabbit/issues/100) - Run Integration Tests in AppVeyor
 - [#99](https://github.com/pardahlman/RawRabbit/issues/99) - Upgrade to RabbitMQ.Client 4.0.X
 - [#98](https://github.com/pardahlman/RawRabbit/issues/98) - Latest version of RabitMQ.Client breaking changes
 - [#97](https://github.com/pardahlman/RawRabbit/issues/97) - Support for topic based routing
 - [#96](https://github.com/pardahlman/RawRabbit/issues/96) - Use RabbitMQ.Client 3.6.4
 - [#92](https://github.com/pardahlman/RawRabbit/issues/92) - Pass GlobalMessageId from Local CallContext
 - [#91](https://github.com/pardahlman/RawRabbit/issues/91) - Update Lifetime Management / Disposal of BusClient

Commits: d9a03a76a2...633dd493cb


# 1.9.5

 - [#96](https://github.com/pardahlman/RawRabbit/issues/96) - Use RabbitMQ.Client 3.6.4
 - [#92](https://github.com/pardahlman/RawRabbit/issues/92) - Pass GlobalMessageId from Local CallContext +feature
 - [#91](https://github.com/pardahlman/RawRabbit/issues/91) - Update Lifetime Management / Disposal of BusClient +feature

Commits: 494e69ba85...c45a2910c6


# 1.9.4

 - [#95](https://github.com/pardahlman/RawRabbit/issues/95) - Upgrade to .NET Core 1.0

Commits: b051f0938d...794dc48506


# 1.9.3

 - [#90](https://github.com/pardahlman/RawRabbit/issues/90) - Default Error Strategy tries to Ack Message twice +fix

Commits: e92022aa6f...186e67157b


# 1.9.2

.NET Core came a step closer to completion With the [announcement of the release of RC2](https://blogs.msdn.microsoft.com/webdev/2016/05/16/announcing-asp-net-core-rc2/). The new releases of Logging, Dependecy Injection and Configuration had a few breaking changes was handled. `RawRabbit` is now fully migrated to the new project structure. There are [new sample projects](https://github.com/pardahlman/RawRabbit/tree/master/sample) that combines .NET Core with RawRabbit (including Attributed Routing, Message Sequence etc.) and Serilog.

The underlying dependecy `RabbitMQ.Client` was updated, as it  [ 3.6.2 was released earlier this week](https://groups.google.com/forum/#!topic/rabbitmq-users/KCtezCXs1l8). While at it, all other NuGet dependencies was updated to its latest version.


 - [#89](https://github.com/pardahlman/RawRabbit/issues/89) - Add vNext Samples
 - [#88](https://github.com/pardahlman/RawRabbit/issues/88) - Upgrade to RabbitMQ.Client 3.6.2
 - [#87](https://github.com/pardahlman/RawRabbit/issues/87) - Implement Timeout for Sequences
 - [#86](https://github.com/pardahlman/RawRabbit/issues/86) - Use dedicated channel for publishing to error exchange
 - [#85](https://github.com/pardahlman/RawRabbit/issues/85) - Upgrade to .NET Core RC2
 - [#83](https://github.com/pardahlman/RawRabbit/issues/83) - Issue when QueueFullName matches an Exchange Name. contributed by ([johnbaker](https://github.com/johnbaker))
 - [#82](https://github.com/pardahlman/RawRabbit/issues/82) - Does RawRabbit runs on DNX Core 5.0 (.Net Core) ?
 - [#81](https://github.com/pardahlman/RawRabbit/issues/81) - Attributes for Routing/Queue/Exchange

Commits: b68cf973fa...e92022aa6f


# 1.9.1

 - [#80](https://github.com/pardahlman/RawRabbit/issues/80) - Use same JsonSerializer throughout the client
 - [#79](https://github.com/pardahlman/RawRabbit/issues/79) - Improve error handling

Commits: 5188354f20...a497734611


# 1.9.0

In this minor release, a breaking change in topology features in introduced, namely the default type for exchanges is `Topic` rather than `Direct`. Read through the [client upgrade](http://rawrabbit.readthedocs.org/en/master/client-upgrade.html) page for more information.

Thanks to [videege](https://github.com/videege), there is now a package for Ninject. We've also added logging adapters for the major logging frameworks (Serilog, NLog and log4net).

 - [#77](https://github.com/pardahlman/RawRabbit/issues/77) - Add logger packages
 - [#76](https://github.com/pardahlman/RawRabbit/issues/76) - Omit slash if virtual host is anything other than default
 - [#75](https://github.com/pardahlman/RawRabbit/issues/75) - Add extension for re-defining topology featuers
 - [#74](https://github.com/pardahlman/RawRabbit/issues/74) - Update MessageSequence Extension
 - [#73](https://github.com/pardahlman/RawRabbit/issues/73) - Append GlobalMessageId to routingkey
 - [#70](https://github.com/pardahlman/RawRabbit/pull/70) - RawRabbit.DependencyInjection.Ninject extension contributed by ([videege](https://github.com/videege))
 - [#69](https://github.com/pardahlman/RawRabbit/issues/69) - Add method to gracefully shut down client
 - [#35](https://github.com/pardahlman/RawRabbit/issues/35) - Return "subscription informaiton" on Subscribe/Respond

Commits: 0207aa75f2...2dc733c28e


# 1.8.13

 - [#72](https://github.com/pardahlman/RawRabbit/issues/72) - Add package for Ninject, contributed by Joshua Barron ([Originalutter](https://github.com/videege))
 - [#71](https://github.com/pardahlman/RawRabbit/issues/71) - Remove QueueingBasicConsumer
 - [#68](https://github.com/pardahlman/RawRabbit/issues/68) - Backward compatible Message Serialization
 - [#65](https://github.com/pardahlman/RawRabbit/issues/65) - Message Type serialization without assembly version, contributed by Marcus Utter ([Originalutter](https://github.com/Originalutter))
 - [#19](https://github.com/pardahlman/RawRabbit/issues/19) - Extension method for message sequences +feature

Commits: b928f96fc8...e1b84ef2f0


# 1.8.12

In this release, the `ChannelFactory` has been rewritten from the ground up. The old channel factory is left intact, but called `ThreadBasedChannelFactory`. One of the sync methods in the `IChannelFactory` interface is removed. It is recommended to use the async methods. All operations now use the new `ITopologyProvider` for creating topology features.

 - [#66](https://github.com/pardahlman/RawRabbit/issues/66) - Upgrade to RabbitMQ.Client 3.6.1
 - [#64](https://github.com/pardahlman/RawRabbit/issues/64) - Refactor Operations: Requester, Responder & Subscriber
 - [#59](https://github.com/pardahlman/RawRabbit/issues/59) - vNext & default ChannelFactory

Commits: bfa88b3083...3e7626fd0d


# 1.8.11

 - [#61](https://github.com/pardahlman/RawRabbit/issues/61) - Improve extraction of Application Name (contributed by ([Originalutter](https://github.com/Originalutter)))
 - [#41](https://github.com/pardahlman/RawRabbit/issues/41) - Correct order of arguments passed to Assert.Equal (contributed by ([Originalutter](https://github.com/Originalutter)))

Commits: e9a1693a0e...8cef898373


# 1.8.10

Improving invokation of Response handler.

 - [#60](https://github.com/pardahlman/RawRabbit/issues/60) - Improve handling multiple RPC Requests

Commits: 444efffedb...e4c3178885


# 1.8.9

Fixes a problem with subscriptions being terminated that was introduced in `1.8.8`.

 - [#58](https://github.com/pardahlman/RawRabbit/issues/58) - Subscribers are terminated +fix

Commits: 8b627ae192...c6f928addf


# 1.8.8

The first step in a refactoring has been taken. The aim is to increase thoughput by using async methods and allowing for multiple threads to publish messages.

Also, a new NuGet package, [`RawRabbit.DependencyInjection.Autofac`](https://www.nuget.org/packages/RawRabbit.DependencyInjection.Autofac) has been created.

 - [#57](https://github.com/pardahlman/RawRabbit/issues/57) - Refactor Operations: Publisher
 - [#55](https://github.com/pardahlman/RawRabbit/issues/55) - Support Autofac

Commits: 8b627ae192...13b86c7bf6


# 1.8.7

In this release the `ConnectionStringParser` has been polished by [Originalutter](https://github.com/Originalutter). It now supports all configuration parameters available in the configuration object. There are also some nice default values, like port `5672` which can be omitted from connection string.

An `KeyNotFound` issue  that sometimes occurred when performing multiple request synchronous (with `await`) was fixed.

 - [#53](https://github.com/pardahlman/RawRabbit/issues/53) - Avoid opening channels on Respond
 - [#50](https://github.com/pardahlman/RawRabbit/issues/50) - Unexpected connection close when multiple RPC
 - [#47](https://github.com/pardahlman/RawRabbit/issues/47) - Add default attributes
 - [#44](https://github.com/pardahlman/RawRabbit/issues/44) - Update QueueArgument with LazyQueue
 - [#42](https://github.com/pardahlman/RawRabbit/issues/42) - Allow connection strings without parameters
 - [#25](https://github.com/pardahlman/RawRabbit/issues/25) - Support more parameters to connectionString

Commits: f0d5128726...09aaea56be
