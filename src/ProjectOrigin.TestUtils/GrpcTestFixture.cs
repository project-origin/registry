#region Copyright notice and license
// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/grpc/test-services/sample/Tests/Server/IntegrationTests/Helpers/GrpcTestFixture.cs
#endregion

using System;
using System.Collections.Generic;
using System.Net.Http;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace ProjectOrigin.TestUtils
{
    public delegate void LogMessage(LogLevel logLevel, string categoryName, EventId eventId, string message, Exception? exception);

    public class GrpcTestFixture<TStartup> : IDisposable where TStartup : class
    {
        private TestServer? _server;
        private IHost? _host;
        private HttpMessageHandler? _handler;
        private GrpcChannel? _channel;
        private Dictionary<string, string?>? _configurationDictionary;

        public event LogMessage? LoggedMessage;
        public GrpcChannel Channel => _channel ??= CreateChannel();

        public Action<IServiceCollection>? testServicesConfigure;

        public GrpcTestFixture()
        {
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(new ForwardingLoggerProvider((logLevel, category, eventId, message, exception) =>
            {
                LoggedMessage?.Invoke(logLevel, category, eventId, message, exception);
            }));
        }

        public T GetRequiredService<T>() where T : class
        {
            EnsureServer();
            return _host!.Services.GetRequiredService<T>();
        }

        public void ConfigureHostConfiguration(Dictionary<string, string?> configuration)
        {
            _configurationDictionary = configuration;
        }

        private void EnsureServer()
        {
            if (_host == null)
            {
                var builder = new HostBuilder();

                if (_configurationDictionary != null)
                {
                    builder.ConfigureHostConfiguration(config =>
                        {
                            config.Add(new MemoryConfigurationSource()
                            {
                                InitialData = _configurationDictionary
                            });
                        });
                }

                builder
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<ILoggerFactory>(LoggerFactory);
                    })
                    .ConfigureWebHostDefaults(webHost =>
                    {
                        webHost
                            .UseTestServer()
                            .UseEnvironment("Development")
                            .UseStartup<TStartup>();

                        if (testServicesConfigure is not null)
                            webHost.ConfigureTestServices(testServicesConfigure);
                    });

                _host = builder.Start();
                _server = _host.GetTestServer();
                _handler = _server.CreateHandler();
            }
        }

        public LoggerFactory LoggerFactory { get; }

        private GrpcChannel CreateChannel()
        {
            EnsureServer();

            return GrpcChannel.ForAddress(_server!.BaseAddress, new GrpcChannelOptions
            {
                LoggerFactory = LoggerFactory,
                HttpHandler = _handler
            });
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _handler?.Dispose();
            _host?.Dispose();
            _server?.Dispose();
        }

        public IDisposable GetTestLogger(ITestOutputHelper outputHelper)
        {
            return new GrpcTestContext<TStartup>(this, outputHelper);
        }
    }
}
