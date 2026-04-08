using Microsoft.EntityFrameworkCore;
using CityFix.Api.Models;

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
    }
}