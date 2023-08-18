using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Xunit;

public class ContainerImageFixture : IAsyncLifetime
{
    public IImage Image => _image;
    private IFutureDockerImage _image;

    public ContainerImageFixture()
    {
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile("ProjectOrigin.Registry.Server/Dockerfile")
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
