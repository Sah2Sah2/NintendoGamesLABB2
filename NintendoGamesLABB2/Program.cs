using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NintendoGamesLABB2.Data;
using NintendoGamesLABB2.Models;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

// CORS allowed from multiple origins to solve the CORS access 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.AllowAnyOrigin()   // Allows all origins
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container
builder.Services.AddAuthorization();

// Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inject MongoCRUD with connection string from environment variables
builder.Services.AddSingleton<MongoCRUD>(new MongoCRUD(builder.Configuration));

var app = builder.Build();

// Enable CORS 
app.UseCors("AllowSpecificOrigins");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();

// POST method to add a new game
app.MapPost("/game", async (NintendoGame game, MongoCRUD db) =>
{
    var addedGame = await db.AddGame("Games", game);
    return Results.Ok(addedGame);
});

// GET all games
app.MapGet("/games", async (MongoCRUD db) =>
{
    var games = await db.GetAllGames("Games");
    return Results.Ok(games);
});

// GET game by ID
app.MapGet("/game/{id}", async (string id, MongoCRUD db) =>
{
    if (!ObjectId.TryParse(id, out ObjectId objectId))
    {
        return Results.BadRequest("Invalid ID format.");
    }

    var game = await db.GetGameById("Games", objectId);
    if (game == null)
        return Results.NotFound($"Game with ID {id} not found.");
    return Results.Ok(game);
});

// GET game by Name 
app.MapGet("/game", async (string name, MongoCRUD db) =>
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest("Game name is required.");
    }

    var games = await db.GetGamesByName("Games", name);

    if (games == null || games.Count == 0)
    {
        return Results.NotFound($"No games found containing '{name}'.");
    }

    return Results.Ok(games);
});

// UPDATE game details
app.MapPut("/game", async (NintendoGame updatedGame, MongoCRUD db) =>
{
    var existingGame = await db.GetGameById("Games", updatedGame.Id);
    if (existingGame == null)
        return Results.NotFound($"Game with ID {updatedGame.Id} not found.");

    var updated = await db.UpdateGame("Games", updatedGame);
    return Results.Ok(updated);
});

// DELETE game by ID
app.MapDelete("/game/{id}", async (string id, MongoCRUD db) =>
{
    if (!ObjectId.TryParse(id, out ObjectId objectId))
    {
        return Results.BadRequest("Invalid ID format.");
    }

    var existingGame = await db.GetGameById("Games", objectId);
    if (existingGame == null)
        return Results.NotFound($"Game with ID {id} not found.");

    var deleted = await db.DeleteGame("Games", objectId);
    return Results.Ok(deleted);
});

app.Run();
