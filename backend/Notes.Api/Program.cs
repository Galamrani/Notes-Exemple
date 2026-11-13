using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<NotesDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Notes")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<NotesService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotesDbContext>().Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
