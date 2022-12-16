using ProjectOrigin.Electricity.Client;
using ProjectOrigin.Electricity.Client.Models;
using ProjectOrigin.Electricity.Server;
using Xunit.Abstractions;

namespace ProjectOrigin.Electricity.IntegrationTests.Helpers
{
    public class RegisterClientTestBase : IntegrationTestBase, IDisposable
    {
        private RegisterClient? _client;
        private SemaphoreSlim _semaphore = new SemaphoreSlim(0, 1);
        private CommandStatusEvent? _result;

        protected RegisterClient Client => _client ??= CreateClient();

        private RegisterClient CreateClient()
        {
            var client = new RegisterClient(Channel);
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

        public RegisterClientTestBase(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper) : base(fixture, outputHelper)
        {
        }

        public new void Dispose()
        {
            _client?.Dispose();
            base.Dispose();
        }
    }
}
