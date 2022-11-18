using Microsoft.Extensions.Options;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;
using ProjectOrigin.Register.LineProcessor.Services;
using ProjectOrigin.Register.LineProcessor.Tests.ExampleChat;
using ProjectOrigin.VerifiableEventStore.Models;
using ProjectOrigin.VerifiableEventStore.Services.Batcher;

namespace ProjectOrigin.Services.Tests;

public class SynchronousCommandStepProcessorTests
{
    [Fact]
    public async Task RequestProcessor_QueueRequest_Success()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var index = fixture.Create<int>();
        var request = NewRequest(registryName);

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<CommandStep>())).ReturnsAsync((new VerificationResult.Valid(), index));
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousCommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);

        await processor.Process(request);

        batcherMock.Verify(obj => obj.PublishEvent(It.Is<VerifiableEvent>(e => e.Id == new EventId(request.FederatedStreamId.StreamId, index))), Times.Once);
    }

    [Fact]
    public async Task RequestProcessor_GetStatus_Error()
    {
        var fixture = new Fixture();
        var registryName = fixture.Create<string>();
        var request = NewRequest(registryName);

        var errorMessage = fixture.Create<string>();

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<CommandStep>())).ReturnsAsync((new VerificationResult.Invalid(errorMessage), fixture.Create<int>()));
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousCommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);
        var result = await processor.Process(request);

        Assert.Equal(CommandStepState.Failed, result.State);
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

        var batcherMock = new Mock<IBatcher>();
        var dispatcherMock = new Mock<ICommandStepDispatcher>();
        dispatcherMock.Setup(obj => obj.Verify(It.IsAny<CommandStep>())).ReturnsAsync((new VerificationResult.Valid(), 0));
        var optionsMock = CreateOptionsMock<RequestProcessorOptions>(new RequestProcessorOptions(registryName));

        var processor = new SynchronousCommandStepProcessor(optionsMock, dispatcherMock.Object, batcherMock.Object);
        var ex = await Assert.ThrowsAsync<InvalidDataException>(() => processor.Process(request));

        Assert.Equal("Invalid registry for request", ex.Message);
    }

    private IOptions<T> CreateOptionsMock<T>(T content) where T : class
    {
        var optionsMock = new Mock<IOptions<T>>();
        optionsMock.Setup(obj => obj.Value).Returns(content);
        return optionsMock.Object;
    }

    private static CommandStep NewRequest(string registry)
    {
        var topic = Guid.NewGuid();
        return new CommandStep<ChatCreatedEvent>(new(registry, topic), new(new ChatCreatedEvent() { Topic = topic.ToString() }, new byte[0]), typeof(Chat), null);
    }
}
