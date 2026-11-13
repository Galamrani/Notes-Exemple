using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Notes.Api.Data;

public class NotesDbContext(DbContextOptions<NotesDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite returns DateTime values with Kind Unspecified; they are always stored as UTC.
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        modelBuilder.Entity<Note>(note =>
        {
            note.Property(n => n.Title).IsRequired().HasMaxLength(100);
            note.Property(n => n.Body).HasMaxLength(2000);
            note.Property(n => n.CreatedAt).HasConversion(utcConverter);
            note.Property(n => n.UpdatedAt).HasConversion(utcConverter);
        });
    }
}
