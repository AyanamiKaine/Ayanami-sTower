using System.Net;
using System.Net.Http.Json;
using AyanamisTower.StellaEcs;
using AyanamisTower.StellaEcs.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AyanamisTower.StellaEcs.RestAPI.IntegrationTests;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class LogsApiIntegrationTests
{
    private static class TestStartup
    {
        public static WebApplication BuildApp(World world)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            var inMemoryProvider = new InMemoryLogProvider(capacity: 256);
            builder.Logging.AddProvider(inMemoryProvider);

            builder.Services.Configure<JsonOptions>(o =>
            {
                o.SerializerOptions.IncludeFields = true;
                o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            builder.Services.AddSingleton(world);
            builder.Services.AddSingleton<ILogStore>(inMemoryProvider);
            builder.Services.AddEndpointsApiExplorer();

            var app = builder.Build();
            app.MapEcsEndpoints();
            return app;
        }
    }

    [Fact]
    public async Task Logs_Filtering_AfterId_Clear_Works()
    {
        // Arrange: host minimal app in-memory
        var world = new World();
        using var app = TestStartup.BuildApp(world);
        await app.StartAsync();
        var client = app.GetTestClient();

        // Generate some logs
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var loggerA = loggerFactory.CreateLogger("CategoryA");
        var loggerB = loggerFactory.CreateLogger("CategoryB");
        loggerA.LogInformation("Hello A1");
        loggerB.LogWarning("Hello B1");
        loggerA.LogError("Hello A2");

        // 1) Get tail (default take)
        var resp1 = await client.GetAsync("/api/logs");
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);
        var jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        jsonOpts.Converters.Add(new JsonStringEnumConverter());
        var entries1 = await resp1.Content.ReadFromJsonAsync<List<LogEntry>>(jsonOpts);
        Assert.NotNull(entries1);
        Assert.True(entries1!.Count >= 3);

        // Record last id
        var lastId = entries1!.Max(e => e.Id);

        // 2) Filter by minLevel=Error
        var resp2 = await client.GetAsync("/api/logs?minLevel=Error");
        var entries2 = await resp2.Content.ReadFromJsonAsync<List<LogEntry>>(jsonOpts);
        Assert.NotNull(entries2);
        Assert.All(entries2!, e => Assert.True(e.Level >= LogLevel.Error));

        // 3) Filter by category substring
        var resp3 = await client.GetAsync("/api/logs?category=CategoryA");
        var entries3 = await resp3.Content.ReadFromJsonAsync<List<LogEntry>>(jsonOpts);
        Assert.NotNull(entries3);
        Assert.All(entries3!, e => Assert.Contains("CategoryA", e.Category));

        // 4) afterId should return only newer ones
        loggerA.LogInformation("Hello A3");
        var resp4 = await client.GetAsync($"/api/logs?afterId={lastId}");
        var entries4 = await resp4.Content.ReadFromJsonAsync<List<LogEntry>>(jsonOpts);
        Assert.NotNull(entries4);
        Assert.All(entries4!, e => Assert.True(e.Id > lastId));

        // 5) clear
        var clear = await client.DeleteAsync("/api/logs");
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);
        // Immediately after clear, the ring should be empty from our provider's perspective
        var resp5 = await client.GetAsync("/api/logs?take=1");
        var entries5 = await resp5.Content.ReadFromJsonAsync<List<LogEntry>>(jsonOpts);
        Assert.NotNull(entries5);
        Assert.Empty(entries5!);

        await app.StopAsync();
    }
}
#pragma warning restore CS1591
