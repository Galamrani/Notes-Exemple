using System.ComponentModel.DataAnnotations;

namespace Notes.Api.Dtos;

public record SaveNoteRequest(string? Title, [MaxLength(2000)] string? Body) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var trimmedLength = Title?.Trim().Length ?? 0;
        if (trimmedLength is < 1 or > 100)
        {
            yield return new ValidationResult(
                "The Title field is required and must be between 1 and 100 characters after trimming.",
                [nameof(Title)]);
        }
    }
}
