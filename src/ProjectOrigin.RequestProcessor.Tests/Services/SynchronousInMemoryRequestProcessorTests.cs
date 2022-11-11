using Microsoft.Extensions.Options;
using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Models;
using ProjectOrigin.RequestProcessor.Services;
using ProjectOrigin.RequestProcessor.Tests.ExampleChat;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.Services.Tests;

public class SynchronousInMemoryRequestProcessorTests
{
    [Fact]
    public async Task RequestProcessor_QueueRequest_Success()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var index = fixture.Create<int>();
        var request = NewRequest(registryName);

        var serializer = new JsonEventSerializer();
        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<IDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<IPublishRequest>())).ReturnsAsync((VerificationResult.Valid, index));
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousInMemoryRequestProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object, serializer);

        await processor.QueueRequest(request);

        batcherMock.Verify(obj => obj.PublishEvent(It.Is<Event>(e => e.Id == new EventId(request.FederatedStreamId.StreamId, index))), Times.Once);
    }

    [Fact]
    public async Task RequestProcessor_GetStatus_Unknown()
    {
        var fixture = new Fixture();

        var registryName = fixture.Create<string>();
        var serializer = new JsonEventSerializer();
        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<IDispatcher>();
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousInMemoryRequestProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object, serializer);

        var result = await processor.GetRequestStatus(fixture.Create<RequestId>());

        Assert.Equal(RequestState.Unknown, result.State);
    }

    [Fact]
    public async Task RequestProcessor_GetStatus_Success()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var request = NewRequest(registryName);

        var serializer = new JsonEventSerializer();
        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<IDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<IPublishRequest>())).ReturnsAsync((VerificationResult.Valid, fixture.Create<int>()));

        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousInMemoryRequestProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object, serializer);
        await processor.QueueRequest(request);

        var result = await processor.GetRequestStatus(request.RequestId);

        Assert.Equal(RequestState.Completed, result.State);
    }

    [Fact]
    public async Task RequestProcessor_GetStatus_Error()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var request = NewRequest(registryName);

        var errorMessage = fixture.Create<string>();

        var serializer = new JsonEventSerializer();
        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<IDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<IPublishRequest>())).ReturnsAsync((VerificationResult.Invalid(errorMessage), fixture.Create<int>()));
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousInMemoryRequestProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object, serializer);
        await processor.QueueRequest(request);

        var result = await processor.GetRequestStatus(request.RequestId);

        Assert.Equal(RequestState.Failed, result.State);
        Assert.Equal(errorMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task RequestProcessor_InvalidRegistry_ThrowsException()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var otherRegistryName = fixture.Create<string>();
        var request = NewRequest(otherRegistryName);
        var errorMessage = fixture.Create<string>();

        var serializer = new JsonEventSerializer();
        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<IDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<IPublishRequest>())).ReturnsAsync((VerificationResult.Valid, 0));
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousInMemoryRequestProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object, serializer);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => processor.QueueRequest(request));

        Assert.Equal("Invalid registry for request", ex.Message);
    }

    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private static ChatCreatedRequest NewRequest(string registry) => new ChatCreatedRequest(new(registry, Guid.NewGuid()), new(Guid.NewGuid()));
}
