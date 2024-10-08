using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ProjectOrigin.Registry.Options;

namespace ProjectOrigin.Registry.MessageBroker;

public sealed class RabbitMqHttpClient : IRabbitMqHttpClient
{
    private const string Protocol = "http";
    private readonly HttpClient _httpClient;
    private static JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public RabbitMqHttpClient(HttpClient httpClient, IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri($"{Protocol}://{rabbitMqOptions.Value.Hostname}:{rabbitMqOptions.Value.HttpApiPort}");

        var byteArray = Encoding.ASCII.GetBytes($"{rabbitMqOptions.Value.Username}:{rabbitMqOptions.Value.Password}");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public async Task<IEnumerable<RabbitMqQueue>> GetQueuesAsync()
    {
        var response = await _httpClient.GetAsync("/api/queues?enable_queue_totals=true&disable_stats=true");
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<IEnumerable<RabbitMqQueue>>(stream, _options)
            ?? throw new FormatException("No queues found");
    }
}
