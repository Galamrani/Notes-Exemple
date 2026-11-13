using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Notes.Api.Dtos;

namespace Notes.Api.Tests;

// xUnit creates a new instance per test, so every test gets its own factory and empty database.
public sealed class NotesEndpointsTests : IDisposable
{
    private readonly NotesApiFactory _factory = new();
    private readonly HttpClient _client;

    public NotesEndpointsTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    public static TheoryData<string, object> InvalidRequests => new()
    {
        { "title missing", new { body = "text" } },
        { "title null", new { title = (string?)null, body = "text" } },
        { "title empty", new { title = "", body = "text" } },
        { "title whitespace", new { title = "   ", body = "text" } },
        { "title 101 characters", new { title = new string('a', 101) } },
        { "title 101 characters after trimming", new { title = "  " + new string('a', 101) + "  " } },
        { "body 2001 characters", new { title = "Valid", body = new string('b', 2001) } },
    };

    [Fact]
    public async Task Post_ValidNote_Returns201WithLocationAndCamelCaseBody()
    {
        var response = await _client.PostAsJsonAsync("/api/notes", new { title = "First", body = "Hello" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(
            ["id", "title", "body", "createdAt", "updatedAt"],
            root.EnumerateObject().Select(p => p.Name).ToArray());
        var id = root.GetProperty("id").GetInt32();
        Assert.Equal("First", root.GetProperty("title").GetString());
        Assert.Equal("Hello", root.GetProperty("body").GetString());
        Assert.EndsWith("Z", root.GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", root.GetProperty("updatedAt").GetString());
        Assert.NotNull(response.Headers.Location);
        Assert.Equal($"/api/notes/{id}", response.Headers.Location.AbsolutePath);
    }

    [Fact]
    public async Task Post_WithoutBody_StoresNullBody()
    {
        var created = await CreateAsync("No body", null);

        Assert.Null(created.Body);
        var fetched = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");
        Assert.Null(fetched!.Body);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Post_InvalidRequest_Returns400ProblemDetails(string _, object request)
    {
        var response = await _client.PostAsJsonAsync("/api/notes", request);

        await AssertValidationProblemAsync(response);
        Assert.Empty((await _client.GetFromJsonAsync<List<NoteResponse>>("/api/notes"))!);
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Put_InvalidRequest_Returns400ProblemDetails(string _, object request)
    {
        var created = await CreateAsync("Original", "Body");

        var response = await _client.PutAsJsonAsync($"/api/notes/{created.Id}", request);

        await AssertValidationProblemAsync(response);
        var fetched = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Post_TitleOf100AndBodyOf2000Characters_IsAccepted()
    {
        var title = new string('t', 100);
        var body = new string('b', 2000);

        var response = await _client.PostAsJsonAsync("/api/notes", new { title, body });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.Equal(title, note!.Title);
        Assert.Equal(body, note.Body);
    }

    [Fact]
    public async Task Put_TitleOf100AndBodyOf2000Characters_IsAccepted()
    {
        var created = await CreateAsync("Original", null);
        var title = new string('t', 100);
        var body = new string('b', 2000);

        var response = await _client.PutAsJsonAsync($"/api/notes/{created.Id}", new { title, body });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.Equal(title, note!.Title);
        Assert.Equal(body, note.Body);
    }

    [Fact]
    public async Task Post_TitleWithSurroundingWhitespace_IsStoredTrimmed()
    {
        var created = await CreateAsync("  Shopping list \t", null);

        Assert.Equal("Shopping list", created.Title);
        var fetched = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");
        Assert.Equal("Shopping list", fetched!.Title);
    }

    [Fact]
    public async Task Put_TitleWithSurroundingWhitespace_IsStoredTrimmed()
    {
        var created = await CreateAsync("Original", null);

        await _client.PutAsJsonAsync($"/api/notes/{created.Id}", new { title = "  Renamed  " });

        var fetched = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");
        Assert.Equal("Renamed", fetched!.Title);
    }

    [Fact]
    public async Task List_EmptyDatabase_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync("/api/notes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_ReturnsNotesNewestCreatedAtFirst()
    {
        var oldest = await CreateAsync("Oldest", null);
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var middle = await CreateAsync("Middle", null);
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        var newest = await CreateAsync("Newest", null);

        // Updating the oldest note must not move it: ordering is by createdAt, not updatedAt.
        _factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await _client.PutAsJsonAsync($"/api/notes/{oldest.Id}", new { title = "Oldest edited" });

        var notes = await _client.GetFromJsonAsync<List<NoteResponse>>("/api/notes");

        Assert.Equal([newest.Id, middle.Id, oldest.Id], notes!.Select(n => n.Id).ToArray());
    }

    [Fact]
    public async Task List_NotesWithSameCreatedAt_AreOrderedByIdDescending()
    {
        var first = await CreateAsync("First", null);
        var second = await CreateAsync("Second", null);

        var notes = await _client.GetFromJsonAsync<List<NoteResponse>>("/api/notes");

        Assert.Equal([second.Id, first.Id], notes!.Select(n => n.Id).ToArray());
    }

    [Fact]
    public async Task Get_ExistingNote_Returns200WithNote()
    {
        var created = await CreateAsync("Read me", "Body");

        var response = await _client.GetAsync($"/api/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.Content.ReadFromJsonAsync<NoteResponse>());
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/notes/999");

        await AssertNotFoundProblemAsync(response);
    }

    [Fact]
    public async Task Put_NonExistentId_Returns404ProblemDetails()
    {
        var response = await _client.PutAsJsonAsync("/api/notes/999", new { title = "Valid" });

        await AssertNotFoundProblemAsync(response);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404ProblemDetails()
    {
        var response = await _client.DeleteAsync("/api/notes/999");

        await AssertNotFoundProblemAsync(response);
    }

    [Fact]
    public async Task Put_ChangesTitleAndBody_UpdatesUpdatedAtAndKeepsCreatedAt()
    {
        var created = await CreateAsync("Before", "Old body");
        _factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var response = await _client.PutAsJsonAsync(
            $"/api/notes/{created.Id}", new { title = "After", body = "New body" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<NoteResponse>();
        Assert.Equal("After", updated!.Title);
        Assert.Equal("New body", updated.Body);
        Assert.True(updated.UpdatedAt > created.UpdatedAt);

        var fetched = await _client.GetFromJsonAsync<NoteResponse>($"/api/notes/{created.Id}");
        Assert.Equal("After", fetched!.Title);
        Assert.Equal("New body", fetched.Body);
        Assert.Equal(created.CreatedAt, fetched.CreatedAt);
        Assert.Equal(updated.UpdatedAt, fetched.UpdatedAt);
        Assert.True(fetched.UpdatedAt > created.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, fetched.CreatedAt.Kind);
    }

    [Fact]
    public async Task Delete_ExistingNote_Returns204AndRemovesNote()
    {
        var keep = await CreateAsync("Keep", null);
        var remove = await CreateAsync("Remove", null);

        var response = await _client.DeleteAsync($"/api/notes/{remove.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/notes/{remove.Id}")).StatusCode);
        var notes = await _client.GetFromJsonAsync<List<NoteResponse>>("/api/notes");
        Assert.Equal([keep.Id], notes!.Select(n => n.Id).ToArray());
    }

    private async Task<NoteResponse> CreateAsync(string title, string? body)
    {
        var response = await _client.PostAsJsonAsync("/api/notes", new { title, body });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<NoteResponse>())!;
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.True(json.RootElement.TryGetProperty("errors", out _));
    }

    private static async Task AssertNotFoundProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
    }
}
