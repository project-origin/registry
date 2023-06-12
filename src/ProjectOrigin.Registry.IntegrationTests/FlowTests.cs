using Xunit.Abstractions;
using ProjectOrigin.TestUtils;
using ProjectOrigin.Registry.Server;

namespace ProjectOrigin.Electricity.IntegrationTests;

public class FlowTests : GrpcTestBase<Startup>
{
    public FlowTests(GrpcTestFixture<Startup> grpcFixture, ITestOutputHelper outputHelper) : base(grpcFixture, outputHelper)
    {
    }
}
