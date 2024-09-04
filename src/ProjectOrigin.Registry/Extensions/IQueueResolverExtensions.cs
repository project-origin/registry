using ProjectOrigin.Registry.MessageBroker;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Registry.Extensions;

public static class IQueueResolverExtensions
{
    public static string GetQueueName(this IQueueResolver queueResolver, Transaction transaction)
    {
        var streamId = transaction.Header.FederatedStreamId.StreamId.ToString();
        return queueResolver.GetQueueName(streamId);
    }
}
