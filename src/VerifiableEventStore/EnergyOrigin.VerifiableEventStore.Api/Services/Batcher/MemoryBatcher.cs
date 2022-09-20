using EnergyOrigin.VerifiableEventStore.Api.Services.BlockchainConnector;
using EnergyOrigin.VerifiableEventStore.Api.Services.EventStore;
using Microsoft.Extensions.Options;

namespace EnergyOrigin.VerifiableEventStore.Api.Services.Batcher;

public class MemoryBatcher : IBatcher
{
    private IBlockchainConnector blockchainConnector;
    private IEventStore eventStore;
    private IOptions<BatcherOptions> options;

    public MemoryBatcher(IBlockchainConnector blockchainConnector, IEventStore eventStore, IOptions<BatcherOptions> options)
    {
        this.blockchainConnector = blockchainConnector;
        this.eventStore = eventStore;
        this.options = options;
    }

    public async Task PublishEvent(PublishEventRequest request)
    {
        throw new NotImplementedException("TODO");
    }
}
