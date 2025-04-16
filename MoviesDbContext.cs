using Microsoft.EntityFrameworkCore;
using MoviesPortal.API.Models;

namespace MoviesPortal.API
{
    public class MoviesDbContext : DbContext
    {
        public MoviesDbContext(DbContextOptions<MoviesDbContext> options) : base(options) { }

        public DbSet<Movie> Movies { get; set; }
    }
}
