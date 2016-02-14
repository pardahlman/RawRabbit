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
