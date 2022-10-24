using ProjectOrigin.RequestProcessor.Interfaces;
using ProjectOrigin.RequestProcessor.Services;
using ProjectOrigin.RequestProcessor.Tests.ExampleChat;

namespace ProjectOrigin.Services.Tests;

public class DispatcherTests
{
    [Fact]
    public async Task Dispatcher_MessagesInSequence_Success()
    {
        var verifiers = new List<Type>(){
            typeof(ChatCreatedVerifier),
            typeof(MessagePostedVerifier)
        };

        var modelLoaderMock = new Mock<IModelLoader>();
        modelLoaderMock.SetupSequence(obj => obj.Get(It.IsAny<Guid>(), It.IsAny<Type>()))
            .ReturnsAsync((Chat.Null, 0))
            .ReturnsAsync((new Chat(), 1));

        var dispatcher = new Dispatcher(verifiers, modelLoaderMock.Object);

        var topicId = Guid.NewGuid();

        var (result1, index1) = await dispatcher.Verify(new ChatCreatedRequest(new FederatedStreamId("", topicId), new ChatCreatedEvent(topicId)));
        Assert.True(result1.IsValid);
        Assert.Equal(0, index1);

        var (result2, index2) = await dispatcher.Verify(new MessagePostedRequest(new FederatedStreamId("", topicId), new MessagePostedEvent(topicId, "hello world")));
        Assert.True(result2.IsValid);
        Assert.Equal(1, index2);
    }

    [Fact]
    public async Task Dispatcher_VerifyFails_ThrowsException()
    {
        var verifiers = new List<Type>(){
            typeof(ChatCreatedVerifier),
            typeof(MessagePostedVerifier)
        };

        var modelLoaderMock = new Mock<IModelLoader>();
        modelLoaderMock.SetupSequence(obj => obj.Get(It.IsAny<Guid>(), It.IsAny<Type>()))
            .ReturnsAsync((Chat.Null, 0))
            .ReturnsAsync((new Chat(), 1));

        var dispatcher = new Dispatcher(verifiers, modelLoaderMock.Object);

        var topicId = Guid.NewGuid();

        var (result, index) = await dispatcher.Verify(new MessagePostedRequest(new FederatedStreamId("", topicId), new MessagePostedEvent(topicId, "hello world")));

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("Invalid request, chat must exist to post message.", result.ErrorMessage);
    }
}
