using System.Collections.Generic;

namespace ProjectOrigin.Registry.Server.Interfaces
{
    public interface IQueueResolver
    {
        string GetQueueName(string streamId);
        string GetQueueName(int server, int verifier);
        IEnumerable<string> GetInactiveQueues(IEnumerable<string> queues);
    }
}
