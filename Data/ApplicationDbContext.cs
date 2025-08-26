using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using google_reviews.Models;

namespace google_reviews.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Review> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Company-Review relationship
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Company)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes for better query performance
            modelBuilder.Entity<Company>()
                .HasIndex(c => c.PlaceId)
                .IsUnique()
                .HasFilter("[PlaceId] IS NOT NULL");

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.CompanyId);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.Time);
        }
    }
}
