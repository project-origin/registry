using Xunit.Abstractions;
using System;
using Xunit;

namespace ProjectOrigin.TestUtils;

public abstract class GrpcTestBase<TStartup> : IClassFixture<GrpcTestFixture<TStartup>>, IDisposable where TStartup : class
{
    protected readonly GrpcTestFixture<TStartup> _grpcFixture;
    private readonly IDisposable _logger;

    public GrpcTestBase(GrpcTestFixture<TStartup> grpcFixture, ITestOutputHelper outputHelper)
    {
        _grpcFixture = grpcFixture;
        _logger = grpcFixture.GetTestLogger(outputHelper);
    }

    public void Dispose()
    {
        _logger.Dispose();
    }
}
