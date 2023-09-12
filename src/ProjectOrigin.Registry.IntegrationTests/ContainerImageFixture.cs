using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Images;
using Xunit;

public class ContainerImageFixture : IAsyncLifetime
{
    private const string DockerfilePath = "ProjectOrigin.Registry.Server/Dockerfile";
    public IImage Image => _image;
    private IFutureDockerImage _image;
    private string _tempDockerPath;

    public ContainerImageFixture()
    {
        // Testcontainers doesn't support buildkit and therefore doesn't support $BUILDPLATFORM
        _tempDockerPath = CreateTempDockerfileWithoutPlatform(Path.Combine(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, DockerfilePath));
        var relativeDockerfile = Path.GetRelativePath(CommonDirectoryPath.GetSolutionDirectory().DirectoryPath, _tempDockerPath);

        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), string.Empty)
            .WithDockerfile(relativeDockerfile)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _image.CreateAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        File.Delete(_tempDockerPath);
        await _image.DeleteAsync().ConfigureAwait(false);
        await _image.DisposeAsync().ConfigureAwait(false);
    }

    private static string CreateTempDockerfileWithoutPlatform(string source)
    {
        var target = $"{source}.tmp";
        File.WriteAllText(target, File.ReadAllText(source).Replace("--platform=$BUILDPLATFORM", ""));
        return target;
    }
}
