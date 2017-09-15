# Modify pre-defined operations

## Understanding Pipes

RawRabbit performs its operations by invoking sequences of middlewares called _pipes_. The `IBusClient` interface contains one method that is used to define a pipe as well as initial values for the pipe context that is being passed and manipulated by middlewares in the pipe

```csharp
public interface IBusClient
{
  Task<IPipeContext> InvokeAsync(Action<IPipeBuilder> pipe, Action<IPipeContext> context, CancellationToken ct);
}
```

Each of the available operations (like `PublishAsync`, `SubscribeAsync` etc) are just calls to `InvokeAsync` with a operation specific, predefined middleware pipe. This section describes how to change the default behaviour of the pre-defined pipes.

## Stage-based middleware injection

The sequence of middleware can be (and often are) devided into multiple stages, indicated by the `StageMarkerMiddleware`. As an example. the [Publish pipe](https://github.com/pardahlman/RawRabbit/blob/2.0/src/RawRabbit.Operations.Publish/PublishMessageExtension.cs) has multiple stages including _ExchangeDeclared_, _ChannelCreated_ and _MessagePublished_. It is possible to inject custom middleware that is executed at each stage. This is done by creating a middleware inherit from `StagedMiddleware`.

```csharp
public class LogAfterPublishMiddleware : StagedMiddleware
{
  // define what stage to inject middleware into
  public override string StageMarker => Pipe.StageMarker.MessagePublished;

  private readonly ILogger _logger = Log.ForContext<LogAfterPublishMiddleware>();

  public override async Task InvokeAsync(IPipeContext context, CancellationToken ct)
  {
    var msgType = context.GetMessageType();
    _logger.Information("Message of type {messageType} just published", msgType.Name);
    await Next.InvokeAsync(context, ct);
  }
}
```

The example allow shows how a logger middleware can be injected after a message has been published. Note that if the pipe does not have a stage that matches the stage marker of the `StagedMiddleware` it will be dismissed for that call.

## Replace pre-defined middleware

In some scenarios it might be desired to change the default behaviour of some core component in all pipes. This can be achieved by creating a _plugin_ that replaces middleware(s) with a custom one(s).

As an example, suppose that you want to make it possible to publish a message with a per call specific message serializer.

```csharp
public class CustomBodySerializationMiddleware : BodySerializationMiddleware
{
  private readonly ISerializer _defaultSerializer;

  public CustomBodySerializationMiddleware(ISerializer serializer) : base(serializer)
  {
    _defaultSerializer = serializer;
  }

  public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
  {
    var message = GetMessage(context);
    var serializer = GetCustomSerializerOrDefault(context);
    var serialized = serializer.Serialize(message);
    AddSerializedMessageToContext(context, serialized);
    await base.InvokeAsync(context, token);
  }

  private ISerializer GetCustomSerializerOrDefault(IPipeContext context)
  {
    return context.GetSerializer() ?? _defaultSerializer;
  }
}
```

The middleware is based on the default `BodySerializationMiddleware`, which has atom methods that can be reused or overriden (as they are marked as virtual) in order to make the custom implementation as small as possible.

See that `context.GetSerializer()` call? That is actually just an extension method to the `IPipeContext` in order to make the code a bit more readable. It is common practice to create descriptive extension methods in order to make it easier to use the feature. The extension methods are typically "use this" and "get this"

```csharp
public static class PipeContextSerializerExtensions
{
  private const string SerializerKey = "Serializer:Custom";

  public static ISerializer GetSerializer(this IPipeContext context)
  {
    return context.Get<ISerializer>(SerializerKey);
  }

  public static IPipeContext UseSerializer(this IPipeContext context, ISerializer serializer)
  {
    context.Properties.AddOrReplace(SerializerKey, serializer);
    return context;
  }
}
```

With the extension methods in place, it will be possible to declare what serializer to use for a specific call

```csharp
await _client.PublishAsync<BasicMessage>(
  new BasicMessage(),
  ctx => ctx.UseSerializer(protobufSerializer)
);
```

In order for the call to work, the middleware needs to be registered in the client, see below.

## Techniques for register middleware

Depending on scenario, it might be desired to register a middleware for each operation performed by the bus client, or just for a specific operation.

### Client wide registration

Middleware can be registered when instanciating the client, thus adding it to the _all_ pipes that are executed. Changes that affect all pipes are called _Plugins_. This can make perfect sense for client wide changes, such as the `CustomBodySerializationMiddleware` middleware. This can be done with the `Replace` call to the `IPipeBuilder`.

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
  {
    Plugins = plugin => plugin.Register(p => p
      .Replace<BodySerializationMiddleware, CustomBodySerializationMiddleware>())
  });
```

In order to make registrations like this easier to grasp, they can be made in an extension method to the `IClientBuilder`

```csharp
public static class CustomSerializerPlugin
{
  public static IClientBuilder UseCustomerSerializerOverride(this IClientBuilder builder)
  {
    builder.Register(pipe => pipe
      .Replace<BodySerializationMiddleware, CustomBodySerializationMiddleware>());
    return builder;
  }
}
```

The client can now be registered like this:

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
  Plugins = plugin => plugin.UseCustomerSerializerOverride()
});
```

### New operation extension method

By creating a new extension method that invokes a pipe a custom middleware, the behavior get isolated to that particular method. The custom pipe can be based on a pre-defined one

```csharp
public static class CustomPublishMiddleware
{
  // define pipe based on existing pipe
  private static readonly Action<IPipeBuilder> CustomPipe = PublishMessageExtension.PublishPipeAction
    + (p =>p.Use<LogAfterPublishMiddleware>());

  public static Task<IPipeContext> PublishCustomAsync<TMessage>(this IBusClient client, TMessage message, CancellationToken ct = default(CancellationToken))
  {
    return client.InvokeAsync(
      CustomPipe,
      ctx =>
      {
        ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
        ctx.Properties.Add(PipeKey.Message, message);
      }, ct);
  }
}
```

In the example above, the extension method `PublishCustomAsync` uses the pre-defined pipe from the Publish operation, but extends it with the `LogAfterPublishMiddleware`. Note that even though the middleware is registered last, it will be moved to the corresponding stage when the pipe is built.