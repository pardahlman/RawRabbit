namespace RawRabbit.Common.Serialization
{
	public interface IMessageSerializer
	{
		byte[] Serialize<T>(T obj);
		T Deserialize<T>(byte[] bytes);
	}
}
