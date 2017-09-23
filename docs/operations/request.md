# Request

The request operation is the initiating part of RPC calls (sometimes refered to as client). It is invoked with two generic arguments, `TRequest` (the outgoing message type) and `TResponse` (the expected response message).

## Default configuration

By default, RawRabbit leverages [direct RPC](https://www.rabbitmq.com/direct-reply-to.html) to enhance performance of the RPC calls. Queues, exchanges and routing keys will be derived from the registered naming convention and configuration object.

## Providing custom configuration

It is possible to provide per-call specific configuration for request queue/exchange as well as expected response queue/exchange. Below is an example

```csharp
var response = await busClient.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), ctx => ctx
  .UseRequestConfiguration(cfg => cfg
    .PublishRequest(p => p
      .OnDeclaredExchange(e => e
        .WithName("custom_exchange")
        .WithAutoDelete())
      .WithRoutingKey("custom_key")
      .WithProperties(prop => prop.DeliveryMode = 1))
    .ConsumeResponse(r => r
      .Consume(c => c
        .WithRoutingKey("response_key"))
      .FromDeclaredQueue(q => q
        .WithName("response_queue")
        .WithAutoDelete())
      .OnDeclaredExchange(e => e
        .WithName("response_exchange")
        .WithAutoDelete()
      )
    )
  ));
```

The fluent configuration provides a clear description of what happens: _"publish the reuqest on a newly declared exchange `custom_exchange` with routing key `routing_key` and expect a response with routing key `response_key` on newly declared queue `response_queue` bound to newly declared exchange `response_exchange`"_.

This is a sub set of configuration available. Other topological features, such as exchange type, exclusive queue etc can be provided in a similar fashion.

## Considerations when using custom response queue

There are a few things to consider before using a custom response queue. A consumer will be created and keep on consuming from that queue indefinitly. This is by design, as each unique queue/routing key/message type call will re-use pre-defined consumer in order to improve throughput and keep resource usage down.

If the configuration above is used in a system with multiple services using a similar configuration, the response queue will get multiple consumers which recive responses in a round-robin'ed manner. This means that a consumer can recieve a response that was intended for a different client.

One way to resolve this issue is to use something like the queue suffix enhancer that adds service name to the queue name, and thus making them unique. If the system has multiple instances of the same service, a custom unique queue suffix or host name suffix might be used to make the response queue unique.

A different way is to use dedicated consumer.

## Dedicated response consumer

Dedicated response consumers are consumers that are created for each RPC request and then disposed after the response is recieved.

```csharp
var response = busClient.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), ctx => ctx
  .UseDedicatedResponseConsumer()
  .UseRequestConfiguration(cfg => cfg
    .ConsumeResponse(r => r
      .Consume(c => c.WithRoutingKey($"response_key_{Guid.NewGuid()}"))
      .FromDeclaredQueue(q => q.WithName($"response_queue_{Guid.NewGuid()}")
      )
    )
  )
);
```
If concurrent RPC calls are made, it is recommended to use a unique response queue as well as routing key to ensure that the response is routed to the expected requester's response consumer.