using Google.Protobuf;
using ProjectOrigin.Register.LineProcessor.Interfaces;
using ProjectOrigin.Register.LineProcessor.Models;
using ProjectOrigin.Register.LineProcessor.Services;
using ProjectOrigin.Register.LineProcessor.Tests.ExampleChat;

namespace ProjectOrigin.Services.Tests;

public class DispatcherTests
{

    private CommandStep<T> Create<T>(Guid id, T a) where T : IMessage
    {
        return new CommandStep<T>(new FederatedStreamId("", id), new SignedEvent<T>(a, new byte[0]), typeof(Chat), null);

    }

    [Fact]
    public async Task Dispatcher_MessagesInSequence_Success()
    {
        var modelLoaderMock = new Mock<IModelLoader>();
        modelLoaderMock.SetupSequence(obj => obj.Get(It.IsAny<FederatedStreamId>(), It.IsAny<Type>()))
            .ReturnsAsync((Chat.Null, 0))
            .ReturnsAsync((new Chat(), 1));

        var dispatcher = new Dispatcher(modelLoaderMock.Object, new FakeVerifierFactory());

        var topicId = Guid.NewGuid();


        var (result1, index1) = await dispatcher.Verify(Create(topicId, new ChatCreatedEvent() { Topic = topicId.ToString() }));
        Assert.IsType<VerificationResult.Valid>(result1);
        Assert.Equal(0, index1);

        var (result2, index2) = await dispatcher.Verify(Create(topicId, new MessagePostedEvent() { Topic = topicId.ToString(), Message = "hello world" }));
        Assert.IsType<VerificationResult.Valid>(result2);
        Assert.Equal(1, index2);
    }

    [Fact]
    public async Task Dispatcher_VerifyFails_ThrowsException()
    {
        var modelLoaderMock = new Mock<IModelLoader>();
        modelLoaderMock.SetupSequence(obj => obj.Get(It.IsAny<FederatedStreamId>(), It.IsAny<Type>()))
            .ReturnsAsync((Chat.Null, 0))
            .ReturnsAsync((new Chat(), 1));

        var dispatcher = new Dispatcher(modelLoaderMock.Object, new FakeVerifierFactory());

        var topicId = Guid.NewGuid();

        var (result, index) = await dispatcher.Verify(Create(topicId, new MessagePostedEvent() { Topic = topicId.ToString(), Message = "hello world" }));

        Assert.IsType<VerificationResult.Invalid>(result);
        var invalidResult = result as VerificationResult.Invalid;
        Assert.NotNull(invalidResult);
        Assert.Equal("Invalid request, chat must exist to post message.", invalidResult!.ErrorMessage);
    }
}
