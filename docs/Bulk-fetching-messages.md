# Bulk-fetching messages

There are times where it is easier to fetch a bunch of messages and process them in a bulk operation, rather than having an active subscriber that processes the messages as they come. This is not part of the core functionality of `RawRabbit`, but exists as a [client extension](https://github.com/pardahlman/RawRabbit/wiki/Extending-RawRabbit) from the [`RawRabbit.Extensions`](https://www.nuget.org/packages/RawRabbit.Exntensions) package.

Getting started with the extensions are easy. Create an bus client using the `RawRabbitFactory.GetExtendableClient()` method. _That's it - you're ready to bulk fetch!_

```csharp
var bulk = client.GetMessages(cfg => cfg
	.ForMessage<BasicMessage>(msg => msg
		.FromQueues("first_queue", "second_queue")
		.WithBatchSize(4))
	.ForMessage<SimpleMessage>(msg => msg
		.FromQueues("another_queue")
		.GetAll()
		.WithNoAck()
	));
```
The fluent builder lets specify what message type you are interested in retrieving, from what queues and how large the batch should be. If you want to get all messages, simple use `GetAll()` and it will empty the queues.

The result contains method for getting messages by type. You can decide for each message if you want to `Ack` it, `Nack` it or put it back in the queue again.

```csharp
var basics = bulk.GetMessages<BasicMessage>()
foreach (var message in basics)
{
	if (CanBeProcessed(message))
	{
		// do stuff
		message.Ack();
	}
	else
	{
		message.Nack();
	}
}
```
If you feel like performing `Ack`/`Nack` the entire bulk, that's fine too
```csharp
bulk.AckAll();
```
Learn more and try it out yourself by running the [`BulkGetTests.cs`](https://github.com/pardahlman/RawRabbit/blob/master/src/RawRabbit.IntegrationTests/Extensions/BulkGetTests.cs)