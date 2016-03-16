# Message Sequence
In many scenarios, it is considered good practice to have an event-driven architecture where message streams of subsequent publish and subscribe moves the business transactions forward. However, there are scenarios where this is not an option. One example is handling web requestes, where the caller synchronously waits for a response.

## Alternative to RPC
Consider a user login scenario that is handled with `UserLoginRequest` and `UserLoginResponse`.

```csharp
// normal rpc response
client.RespondAsync<UserLoginRequest, UserLoginResponse>(async (request, context) =>
{
	var result = await Authenticate();
	return new UserLoginResponse {Token = result};
});

// normal rpc request
var respons = await client.RequestAsync<UserLoginRequest, UserLoginResponse>();
```

There are a few drawbacks of using this pattern. The way `RPC` is implemented with a private response queue, alternativly a direct-rpc queue, makes the calls private between the requester and responder. This is where the `MessageSequence` extension can be useful. 

```csharp
// normal subscribe
client.SubscribeAsync<UserLoginRequest>(async (msg, context) =>
{
	var result = await Authenticate();
	await client.PublishAsync(new UserLoginResponse { Token = result}, context.GlobalMessageId);
});

// equivalent message sequence
var sequence = _client.ExecuteSequence(c => c
	.PublishAsync<UserLoginRequest>()
	.Complete<UserLoginResponse>()
);
```

The return object is a `MessageSequence<TComplete>` where `<TComplete>` is the generic type of the `.Complete<TComplete>` call. The sequence has a `Task<TComplete>` that completes as the `UserLoginResponse` is published. The major difference is that the message sequence rely on the message context's `GlobalRequestId` to match the response to the request, rather than having a private response queue or correlation id. The recieving end of the `UserLoginRequest` looks like this

One of the benifits is that the message sequence "response" is actually a publish that is published on the exchange according to the registered `INamingConvention`. That means that any other subscribers of the `LoginResponse` can act upon the message.

## Multi-message sequence

The `MessageSequence` extension provides methods to act upon multiple events.

```csharp
var chain = _client.ExecuteSequence(c => c
	.PublishAsync<UserLoginAttempted>()
	.When<UserGeograficPosition>((msg, ctx) => ActOnGeograficPosition(msg.Position))
	.When<UserContactDetail>((msg, ctx) => ActOnContactDetails(msg.Details))
	.Complete<UserLoggoedIn>()
);
```

### Optional messages in chain
The `When` call has an optional parameter that can be used to mark a step in the sequence as optional, meaning that if a message that corresponds to a step later in the sequence is recieved, it skips that step.

```csharp
var chain = _client.ExecuteSequence(c => c
	.PublishAsync<UserLoginAttempted>()
	.When<UserPasswordIsWeak>(
		(msg, ctx) => PromptChangePassword(),
		(cfg) => cfg.IsOptional())
	.Complete<UserLoggoedIn>()
);
```

### Abort sequence premature
The optional parameter for the `When` also have a method to indicate that if the messagee is recieved, it aborts the execution of the sequence. All handlers that are marked as aborting execution is by default optional.

```csharp
var chain = _client.ExecuteSequence(c => c
	.PublishAsync<UserLoginAttempted>()
	.When<UserLoginFailed>(
		(msg, ctx) => PromptChangePassword(),
		(cfg) => cfg.AbortsExecution())
	.Complete<UserLoggoedIn>()
);
```