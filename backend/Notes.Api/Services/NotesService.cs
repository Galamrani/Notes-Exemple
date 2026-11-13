using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Dtos;

namespace Notes.Api.Services;

public class NotesService(NotesDbContext db, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<NoteResponse>> ListAsync()
    {
        var notes = await db.Notes
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .ToListAsync();
        return notes.Select(ToResponse).ToList();
    }

    public async Task<NoteResponse?> GetAsync(int id)
    {
        var note = await db.Notes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return note is null ? null : ToResponse(note);
    }

    public async Task<NoteResponse> CreateAsync(SaveNoteRequest request)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var note = new Note
        {
            Title = request.Title!.Trim(),
            Body = request.Body,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync();
        return ToResponse(note);
    }

    public async Task<NoteResponse?> UpdateAsync(int id, SaveNoteRequest request)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null)
        {
            return null;
        }

        note.Title = request.Title!.Trim();
        note.Body = request.Body;
        note.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
        return ToResponse(note);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var note = await db.Notes.FindAsync(id);
        if (note is null)
        {
            return false;
        }

        db.Notes.Remove(note);
        await db.SaveChangesAsync();
        return true;
    }

    private static NoteResponse ToResponse(Note note) =>
        new(note.Id, note.Title, note.Body, note.CreatedAt, note.UpdatedAt);
}
