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
