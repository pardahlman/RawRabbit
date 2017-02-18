# Http Context Enricher

The _Http Context Enrichers_ adds the current `HttpContext` to the `IPipeContext`, making it available for middlewares throughout the middleware execution chain. This can be helpful for web apps that want to act on request URLs, cookies or other http related properties.

For applications running the full .NET framework, it uses `HttpContext.Current`, for .NET Core it leverages `IHttpContextAccesor` and has a dependency to `Microsoft.AspNetCore.Mvc.Core`.

```csharp
public override Task InvokeAsync(IPipeContext context, CancellationToken token)
{
    var httpContext = context.GetHttpContext();
    return Next.InvokeAsync(context, token);
}
```

## Register as a plugin

If desired, the enricher can be registered as a plugin, making it available for each pipe execution.

```csharp
var options = new RawRabbitOptions
{
    Plugins = p => p.UseHttpContext()
};
```

## Manual registration

The `HttpContext` can be manualy registered in the pipe context action, for example from `ApiControllers`.

```csharp
await _busClient.PublishAsync(
    new ValuesRequested(),
    ctx => ctx.UseHttpContext(HttpContext)
);
```

## Example: Message Context

The _Http Context Enricher_ can be combined with the _Message Context Enricher_ to create custom message contexts.

```csharp
public class CustomContext
{
    public string SessionId { get; set; }
    public string Source { get; set; }
    public Guid ExecutionId { get; set; }
}
```

```csharp
var client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
{
    Plugins = p => p
        .UseHttpContext()
        .UseMessageContext(ctx => new CustomContext
        {
            SessionId = ctx.GetHttpContext().Request.Cookies["rawrabbit:sessionid"],
            Source = ctx.GetHttpContext().Request.GetDisplayUrl(),
            ExecutionId = Guid.NewGuid()
        })
});
```

Note that the plugins are registered sequentially, and in order for this to work, the _Http Context Plugin_ needs to be registered before the _Message Context Plugin_.