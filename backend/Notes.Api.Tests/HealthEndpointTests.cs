using System.Net;
using System.Text.Json;

namespace Notes.Api.Tests;

public class HealthEndpointTests(NotesApiFactory factory) : IClassFixture<NotesApiFactory>
{
    [Fact]
    public async Task GetHealth_ReturnsOkWithStatusOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var properties = json.RootElement.EnumerateObject().ToList();
        var status = Assert.Single(properties);
        Assert.Equal("status", status.Name);
        Assert.Equal("ok", status.Value.GetString());
    }
}
