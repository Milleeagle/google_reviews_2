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
        public DbSet<ScheduledReviewMonitor> ScheduledReviewMonitors { get; set; }
        public DbSet<ScheduledMonitorCompany> ScheduledMonitorCompanies { get; set; }
        public DbSet<ScheduledMonitorExecution> ScheduledMonitorExecutions { get; set; }

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

            // Configure ScheduledReviewMonitor relationships
            modelBuilder.Entity<ScheduledMonitorCompany>()
                .HasOne(smc => smc.ScheduledReviewMonitor)
                .WithMany(srm => srm.Companies)
                .HasForeignKey(smc => smc.ScheduledReviewMonitorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScheduledMonitorCompany>()
                .HasOne(smc => smc.Company)
                .WithMany()
                .HasForeignKey(smc => smc.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ScheduledMonitorExecution>()
                .HasOne(sme => sme.ScheduledReviewMonitor)
                .WithMany(srm => srm.Executions)
                .HasForeignKey(sme => sme.ScheduledReviewMonitorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes for scheduled monitors
            modelBuilder.Entity<ScheduledReviewMonitor>()
                .HasIndex(srm => srm.NextRunAt);

            modelBuilder.Entity<ScheduledReviewMonitor>()
                .HasIndex(srm => srm.IsActive);

            modelBuilder.Entity<ScheduledMonitorExecution>()
                .HasIndex(sme => sme.ExecutedAt);
        }
    }
}
