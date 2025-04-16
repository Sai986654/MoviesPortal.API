using Microsoft.EntityFrameworkCore;
using MoviesPortal.API.Models;
using System;

namespace MoviesPortal.API
{
    public static class MoviesApi
    {
        public static void MapMoviesEndpoints(this WebApplication app)
        {
            // GET /api/movies with try-catch
            app.MapGet("/api/movies", async (MoviesDbContext db) =>
            {
                try
                {
                    return await db.Movies.ToListAsync();
                }
                catch (Exception ex)
                {
                    // Log the exception (ensure ILogger is injected or use a logging framework)
                    // Example: app.Logger.LogError(ex, "Error retrieving movies");
                    return Results.Problem($"Error: {ex.Message}");
                }
            });

            // POST /api/movies with try-catch
            app.MapPost("/api/movies", async (MoviesDbContext db, Movie movie) =>
            {
                try
                {
                    db.Movies.Add(movie);
                    await db.SaveChangesAsync();
                    return Results.Created($"/api/movies/{movie.Id}", movie);
                }
                catch (Exception ex)
                {
                    // Log the exception
                    // Example: app.Logger.LogError(ex, "Error adding movie");
                    return Results.Problem($"Error: {ex.Message}");
                }
            });

            // Test endpoint
            app.MapGet("/", () => "Hello, this app is working! - April 16, 2025");
        }
    }
}
