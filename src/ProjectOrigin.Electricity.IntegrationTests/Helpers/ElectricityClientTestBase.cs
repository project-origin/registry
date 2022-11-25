using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.Electricity.Server;
using Xunit.Abstractions;

namespace ProjectOrigin.Electricity.IntegrationTests.Helpers
{
    public class ElectricityClientTestBase : IntegrationTestBase, IDisposable
    {
        private ElectricityClient? _client;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
        private CommandStatusEvent? _result;

        protected ElectricityClient Client => _client ??= CreateClient();

        private ElectricityClient CreateClient()
        {
            var client = new ElectricityClient(Channel);
            client.Events += (result) =>
            {
                _result = result;
                _semaphore.Release();
            };

            return client;
        }

        protected async Task<CommandStatusEvent?> GetResult()
        {
            await _semaphore.WaitAsync();
            return _result;
        }

        public ElectricityClientTestBase(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
        {
        }

        public new void Dispose()
        {
            _client?.Dispose();
            base.Dispose();
        }
    }
}
