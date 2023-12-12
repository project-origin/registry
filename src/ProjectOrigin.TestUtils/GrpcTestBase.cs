using Xunit.Abstractions;
using System;
using Xunit;

namespace ProjectOrigin.TestUtils;

public abstract class GrpcTestBase<TStartup> : IClassFixture<GrpcTestFixture<TStartup>>, IDisposable where TStartup : class
{
    protected readonly GrpcTestFixture<TStartup> _grpcFixture;
    private readonly IDisposable _logger;
    private bool _disposed = false;

    public GrpcTestBase(GrpcTestFixture<TStartup> grpcFixture, ITestOutputHelper outputHelper)
    {
        _grpcFixture = grpcFixture;
        _logger = grpcFixture.GetTestLogger(outputHelper);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _logger.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~GrpcTestBase()
    {
        Dispose(false);
    }
}
