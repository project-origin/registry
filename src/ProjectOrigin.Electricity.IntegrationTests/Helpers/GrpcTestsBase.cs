using ProjectOrigin.Electricity.IntegrationTests.TestClassFixtures;
using ProjectOrigin.Electricity.Server;
using Xunit.Abstractions;
using System;
using Xunit;

namespace ProjectOrigin.Electricity.IntegrationTests;

public abstract class GrpcTestsBase : IClassFixture<GrpcTestFixture<Startup>>, IDisposable
{
    protected readonly GrpcTestFixture<Startup> _grpcFixture;
    private readonly IDisposable _logger;

    public GrpcTestsBase(GrpcTestFixture<Startup> grpcFixture, ITestOutputHelper outputHelper)
    {
        _grpcFixture = grpcFixture;
        _logger = grpcFixture.GetTestLogger(outputHelper);
    }

    public void Dispose()
    {
        _logger.Dispose();
    }
}
