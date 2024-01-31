using ProjectOrigin.Registry.Server.Interfaces;
using ProjectOrigin.Registry.V1;

namespace ProjectOrigin.Registry.Server.Extensions;

public static class IQueueResolverExtensions
{
    public static string GetQueueName(this IQueueResolver queueResolver, Transaction transaction)
    {
        var streamId = transaction.Header.FederatedStreamId.StreamId.ToString();
        return queueResolver.GetQueueName(streamId);
    }
}
