using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using ProjectOrigin.Registry.IntegrationTests.Extensions;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests.Fixtures;

public class ContainerImageFixture : IAsyncLifetime
{
    private const string DockerfilePath = "Registry.Dockerfile";
    public IImage Image => _image;
    private IFutureDockerImage _image;

    public ContainerImageFixture()
    {
        var dockerfileContent = File.ReadAllText(Path.Combine(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, DockerfilePath));
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileContent(dockerfileContent.Replace(" --platform=$BUILDPLATFORM", ""))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _image.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await _image.DeleteAsync();
        await _image.DisposeAsync();
    }
}
