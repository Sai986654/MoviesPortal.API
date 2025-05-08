using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoviesPortal.API.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MoviesPortal.API
{
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        public static void EnsureDatabaseMigrated(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var dbContext = services.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate(); // Applies any pending migrations to the database
            }
            catch (Exception ex)
            {
                // Log the exception (ensure ILogger is configured)
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }
    }

    public static class MoviesApi
    {
        public static void MapMoviesEndpoints(this WebApplication app, IConfiguration configuration)
        {

            // Test endpoint
            app.MapGet("/", () => "Hello, this app is working! - April 16, 2025");

            // GET /api/movies with try-catch
            app.MapGet("/api/movies", async (HttpContext context) =>
            {
                var db = context.RequestServices.GetRequiredService<AppDbContext>();
                try
                {
                    return Results.Ok(await db.Movies.ToListAsync());
                }
                catch (Exception ex)
                {
                    // Log the exception (ensure ILogger is injected or use a logging framework)
                    // Example: app.Logger.LogError(ex, "Error retrieving movies");
                    return Results.Problem($"Error: {ex.Message}");
                }
            });

            // POST /api/movies with try-catch
            app.MapPost("/api/movies", async (HttpContext context, Movie movie) =>
            {
                var db = context.RequestServices.GetRequiredService<AppDbContext>();
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

            app.MapDelete("/api/movies/{id}", async (int id, AppDbContext db) =>
            {
                var movie = await db.Movies.FindAsync(id);
                if (movie is null) return Results.NotFound();
                db.Movies.Remove(movie);
                await db.SaveChangesAsync();
                return Results.Ok();
            });

            app.MapPut("/api/movies/{id}", async (int id, Movie updatedMovie, AppDbContext db) =>
            {
                var movie = await db.Movies.FindAsync(id);
                if (movie is null) return Results.NotFound();
                movie.Title = updatedMovie.Title;
                movie.Genre = updatedMovie.Genre;
                movie.ReleaseDate = updatedMovie.ReleaseDate;
                await db.SaveChangesAsync();
                return Results.Ok(movie);
            });

            app.MapPost("/api/auth/register", async (
                                                RegisterModel model,
                                                UserManager<IdentityUser> userManager,
                                                RoleManager<IdentityRole> roleManager) =>
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);

                if (!await roleManager.RoleExistsAsync(model.Role))
                {
                    await roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, model.Role);
                    return Results.Ok(new { message = "User registered successfully" });
                }

                return Results.BadRequest(result.Errors);
            });

            // POST /api/auth/login
            app.MapPost("/api/auth/login", async (
                                                RegisterModel model,
                                                UserManager<IdentityUser> userManager,
                                                SignInManager<IdentityUser> signInManager,
                                                IConfiguration config) =>
            {
                var user = await userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    return Results.Unauthorized();
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                {
                    return Results.Unauthorized();
                }

                var userRoles = await userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Add roles as claims
                foreach (var role in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));

                var token = new JwtSecurityToken(
                    issuer: config["Jwt:Issuer"],
                    audience: config["Jwt:Audience"],
                    expires: DateTime.UtcNow.AddHours(2),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Results.Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    username = user.UserName,
                    roles = userRoles.FirstOrDefault()
                });
            });
        }
    }
}