using System;
using System.IO;
using DotNet.Testcontainers.Builders;

namespace ProjectOrigin.Registry.IntegrationTests.Extensions;

public static class ImageFromDockerfileBuilderExtensions
{
    public static ImageFromDockerfileBuilder WithDockerfileContent(this ImageFromDockerfileBuilder image, string dockerfileContent)
    {
        var tempfolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var filename = Guid.NewGuid().ToString() + ".Dockerfile";

        Directory.CreateDirectory(tempfolder);
        File.WriteAllText(Path.Combine(tempfolder, filename), dockerfileContent);

        return image
            .WithDockerfileDirectory(tempfolder)
            .WithDockerfile(filename);
    }
}
