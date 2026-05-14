using Microsoft.EntityFrameworkCore;
using CityFix.Api.Models;
using NetTopologySuite.Geometries;
namespace CityFix.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Worker> Workers => Set<Worker>();
        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<PasswordResetCode> PasswordResetCodes => Set<PasswordResetCode>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<ReportStatusHistory> ReportStatusHistories => Set<ReportStatusHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("postgis");

            modelBuilder.Entity<Report>()
                .Property(r => r.LocationPoint)
                .HasColumnType("geometry(Point,4326)");

            modelBuilder.Entity<Report>()
                .HasIndex(r => r.LocationPoint)
                .HasMethod("GIST");
        }
    }
}