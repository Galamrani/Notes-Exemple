using Microsoft.AspNetCore.Mvc;
using Notes.Api.Dtos;
using Notes.Api.Services;

namespace Notes.Api.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController(NotesService notes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NoteResponse>>> List() => Ok(await notes.ListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NoteResponse>> Get(int id)
    {
        var note = await notes.GetAsync(id);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpPost]
    public async Task<ActionResult<NoteResponse>> Create(SaveNoteRequest request)
    {
        var note = await notes.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NoteResponse>> Update(int id, SaveNoteRequest request)
    {
        var note = await notes.UpdateAsync(id, request);
        return note is null ? NotFound() : Ok(note);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id) =>
        await notes.DeleteAsync(id) ? NoContent() : NotFound();
}
