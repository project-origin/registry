using System.Collections.Generic;

namespace ProjectOrigin.Registry.MessageBroker
{
    public interface IQueueResolver
    {
        string GetQueueName(string streamId);
        string GetQueueName(int server, int verifier);
        IEnumerable<string> GetInactiveQueues(IEnumerable<string> queues);
    }
}
