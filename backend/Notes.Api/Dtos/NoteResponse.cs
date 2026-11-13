namespace Notes.Api.Dtos;

public record NoteResponse(int Id, string Title, string? Body, DateTime CreatedAt, DateTime UpdatedAt);
