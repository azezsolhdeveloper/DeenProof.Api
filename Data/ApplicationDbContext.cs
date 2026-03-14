using DeenProof.Api.Entities;
using Microsoft.EntityFrameworkCore;
namespace DeenProof.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Doubt> Doubts { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Source> Sources { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }

    }
}
