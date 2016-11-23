namespace RawRabbit.Extensions.BulkGet.Model
{
    public interface IBulkMessage
    {
        void Ack();
        void Nack(bool requeue = true);
    }
}