using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Xunit;

namespace ProjectOrigin.Registry.IntegrationTests.Fixtures;

public class ContainerImageFixture : IAsyncLifetime
{
    private const string DockerfilePath = "Registry.Dockerfile";
    public IImage Image => _image;
    private IFutureDockerImage _image;
    private string _tempDockerPath;

    public ContainerImageFixture()
    {
        var folder = Path.Combine(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath);

        // Testcontainers doesn't support buildkit and therefore doesn't support $BUILDPLATFORM
        _tempDockerPath = CreateTempDockerfileWithoutPlatform(Path.Combine(folder, DockerfilePath));
        var relativeDockerfile = Path.GetRelativePath(folder, _tempDockerPath);

        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(folder)
            .WithDockerfile(relativeDockerfile)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _image.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        File.Delete(_tempDockerPath);
        await _image.DeleteAsync();
        await _image.DisposeAsync();
    }

    private static string CreateTempDockerfileWithoutPlatform(string source)
    {
        var target = $"{source}.tmp";

        var content = File.ReadAllText(source)
            .Replace(" --platform=$BUILDPLATFORM", "") // not supported by Testcontainers
            .Replace("-noble-chiseled-extra", ""); // not supported by Testcontainers because of user permissions

        File.WriteAllText(target, content);
        return target;
    }
}
