namespace RawRabbit.Configuration.Queue
{
	public interface IQueueDeclarationBuilder
	{
		IQueueDeclarationBuilder WithName(string queueName);
		IQueueDeclarationBuilder WithAutoDelete(bool autoDelete = true);
		IQueueDeclarationBuilder WithDurability(bool durable = true);
		IQueueDeclarationBuilder WithExclusivity(bool exclusive = true);
		IQueueDeclarationBuilder WithArgument(string key, object value);

		IQueueDeclarationBuilder WithNameSuffix(string suffix);
	}
}
