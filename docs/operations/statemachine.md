# State Machine

RawRabbit State Machine is an instrument of defining complex distributed in time workflows. Each state machine has state (Model) and triggers to control state machine transitions. Raw Rabbit State Machine operation heavily based on Stateless library, enchanced with functionality of "wiring" state machine triggers to RabbitMQ queue messages.

## Configuration

To start using RawRabbit State Machine operation you need to install RawRabbit.Operations.StateMachine nuget package.
On registration of RawRabbit

```csharp
services.AddRawRabbit(new RawRabbitOptions
{
	Plugins = p =>
	{
		p.UseStateMachine();
	}
});
```

Then you need to define state machine itself and all it's artifacts:
## State machine state*

public enum State
	{
		Created,
		InProgress,
		Paused,
		Completed,
		Aborted
	}

### State machine state model

Defines state machine instance

```csharp
public class GenericProcessModel : Model<State>
{
	public string Assignee { get; set; }
	public string Name { get; set; }
	public DateTime Deadline { get; set; }
}
```

### State machine triggers*
public enum Trigger
{
	Start,
	Cancel,
	Completion,
	Pausing,
	Resuming
}
* Note: using enum for States and Triggers is not mandatory. It can be a custom implementation of enumeration if needed. E.g. https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types

## State machine definition

Inherit StateMachineBase<TState, TTrigger, TStateModel>

```csharp
public class GenericProcess : StateMachineBase<State, Trigger, GenericProcessModel>
```
To define state transitions of the state machine, ConfigureState method needs to be overriden
```csharp
protected override void ConfigureState(StateMachine<State, Trigger> process)
{
	_cancel = process.SetTriggerParameters<string>(Trigger.Cancel);
	_pause = process.SetTriggerParameters<string>(Trigger.Pausing);

	process
		.Configure(State.Created)
		.PermitIf(Trigger.Start, State.InProgress, IsAssigned)
		.Permit(Trigger.Cancel, State.Aborted);

	process
		.Configure(State.InProgress)
		.OnEntryAsync(() => SendUpdateMessage())
		.Permit(Trigger.Completion, State.Completed)
		.Permit(Trigger.Pausing, State.Paused)
		.Permit(Trigger.Cancel, State.Aborted);
		....
}
```
## Trigger configuration
To bind your state machine to the message source you can define a trigger configuration
```csharp
public class ProcessTriggers : TriggerConfigurationCollection
{
	public override void ConfigureTriggers(TriggerConfigurer trigger)
	{
		trigger
			.FromMessage<GenericProcess, CreateTask>(
				process => Guid.NewGuid(),
				(task, msg) => task.CreateAsync(msg.Name, msg.DeadLine))
```					
where
CreateTask is an type of msg which will be received from RabbitMQ
task is an instance of GenericProcess state machine
Guid.NewGuid() is a correlation function. Here initial collelation id is generated. Later on in the flow it may look like:
```csharp
.FromMessage<GenericProcess, StartTask>(
					start => start.TaskId,
					(task, msg) => task.StartAsync(msg.Assignee))
```
