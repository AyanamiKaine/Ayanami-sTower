using System.Net;
using System.Net.Http.Json;
using AyanamisTower.StellaEcs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AyanamisTower.StellaEcs.RestAPI.IntegrationTests;

#pragma warning disable CS1591 // XML docs

public class EntitiesApiIntegrationTests
{
    private static WebApplication BuildApp(World world)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.Configure<JsonOptions>(o =>
        {
            o.SerializerOptions.IncludeFields = true;
            o.SerializerOptions.PropertyNameCaseInsensitive = true;
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddSingleton(world);
        builder.Services.AddEndpointsApiExplorer();

        var app = builder.Build();
        AyanamisTower.StellaEcs.Api.EcsApiEndpoints.MapEcsEndpoints(app);
        return app;
    }

    private sealed class EntityDetailDto
    {
        public uint Id { get; set; }
        public List<ComponentInfoDto> Components { get; set; } = new();
    }
    private sealed class ComponentInfoDto
    {
        public string TypeName { get; set; } = string.Empty;
        public object? Data { get; set; }
        public string? PluginOwner { get; set; }
        public bool IsDynamic { get; set; }
    }

    [Fact]
    public async Task GetEntityDetails_ShouldReturn200_ForValidEntity()
    {
        var world = new World();
        var e = world.CreateEntity();

        using var app = BuildApp(world);
        await app.StartAsync();
        var client = app.GetTestClient();

        var resp = await client.GetAsync($"/api/entities/{e.Id}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var dto = await resp.Content.ReadFromJsonAsync<EntityDetailDto>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(dto);
        Assert.Equal(e.Id, dto!.Id);

        await app.StopAsync();
    }

    [Fact]
    public async Task GetEntityDetails_ShouldReturn404_ForInvalidId()
    {
        var world = new World();
        // No entities created yet, id 999 should be invalid

        using var app = BuildApp(world);
        await app.StartAsync();
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/api/entities/999");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);

        await app.StopAsync();
    }
}

#pragma warning restore CS1591
