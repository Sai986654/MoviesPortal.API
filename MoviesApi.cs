using Microsoft.EntityFrameworkCore;
using MoviesPortal.API.Models;
using System;

namespace MoviesPortal.API
{
    public static class MoviesApi
    {
        public static void MapMoviesEndpoints(this WebApplication app)
        {
            app.MapGet("/api/movies", async (MoviesDbContext db) =>
            await db.Movies.ToListAsync());

            app.MapPost("/api/movies", async (MoviesDbContext db, Movie movie) =>
            {
                db.Movies.Add(movie);
                await db.SaveChangesAsync();
                return Results.Created($"/api/movies/{movie.Id}", movie);
            });

            // New endpoint to test if the app is working
            app.MapGet("/", () => "Hello, this app is working! - April 16, 2025");

        }
    }

}
