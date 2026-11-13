namespace Notes.Api.Data;

public class Note
{
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
