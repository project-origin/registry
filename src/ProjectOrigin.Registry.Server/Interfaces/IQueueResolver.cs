namespace ProjectOrigin.Registry.Server.Interfaces
{
    public interface IQueueResolver
    {
        string GetQueueName(string streamId);
        string GetQueueName(int server, int verifier);
    }
}
