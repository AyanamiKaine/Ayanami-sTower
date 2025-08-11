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

#pragma warning disable CS1591

public class DynamicApiIntegrationTests
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

    [Fact]
    public async Task SetGetRemove_Dynamic_On_Entity()
    {
        var world = new World();
        var e = world.CreateEntity();

        using var app = BuildApp(world);
        await app.StartAsync();
        var client = app.GetTestClient();

        // Set
        var setResp = await client.PostAsJsonAsync($"/api/entities/{e.Id}/dynamic/Tag", new { value = 123, note = "hi" });
        Assert.Equal(HttpStatusCode.OK, setResp.StatusCode);

        // Get
        var getResp = await client.GetAsync($"/api/entities/{e.Id}/dynamic/Tag");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var value = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(value.ValueKind == JsonValueKind.Object);

        // Remove
        var delResp = await client.DeleteAsync($"/api/entities/{e.Id}/dynamic/Tag");
        Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);

        // Now GET should be 204
        var getResp2 = await client.GetAsync($"/api/entities/{e.Id}/dynamic/Tag");
        Assert.Equal(HttpStatusCode.NoContent, getResp2.StatusCode);

        await app.StopAsync();
    }

    [Fact]
    public async Task Query_Dynamic_By_Names()
    {
        var world = new World();
        var a = world.CreateEntity().SetDynamic("A", null).SetDynamic("B", null);
        var b = world.CreateEntity().SetDynamic("A", null);
        world.CreateEntity(); // c with no dynamics

        using var app = BuildApp(world);
        await app.StartAsync();
        var client = app.GetTestClient();

        var resp = await client.GetAsync("/api/query/dynamic?names=A,B");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var ids = await resp.Content.ReadFromJsonAsync<List<uint>>();
        Assert.NotNull(ids);
        Assert.Contains(a.Id, ids!);
        Assert.DoesNotContain(b.Id, ids!); // b misses B

        await app.StopAsync();
    }
}

#pragma warning restore CS1591
