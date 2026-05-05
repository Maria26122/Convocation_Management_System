using Convocation.Entities;
using Convocation_Management_System.Web.UI.Models;
using Microsoft.EntityFrameworkCore;

namespace Convocation.DataAccess
{
    public class ConvocationDbContext : DbContext
    {
        public ConvocationDbContext(DbContextOptions<ConvocationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Role { get; set; }
        public DbSet<UserAccount> UserAccount { get; set; }
        public DbSet<Participant> Participant { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Registration> Registration { get; set; }
        public DbSet<Guest> Guest { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<QrPass> QrPass { get; set; }
        public DbSet<DistributionLog> DistributionLog { get; set; }
        public DbSet<Permission> Permission { get; set; }
        public DbSet<RolePermission> RolePermission { get; set; }
        public DbSet<StaffTask> StaffTask { get; set; }
        public DbSet<UserPermission> UserPermission { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<UserAccount>().ToTable("UserAccount");
            modelBuilder.Entity<Participant>().ToTable("Participant");
            modelBuilder.Entity<Event>().ToTable("Event");
            modelBuilder.Entity<Registration>().ToTable("Registration");
            modelBuilder.Entity<Guest>().ToTable("Guest");
            modelBuilder.Entity<Payment>().ToTable("Payment");
            modelBuilder.Entity<QrPass>().ToTable("QrPass");
            modelBuilder.Entity<DistributionLog>().ToTable("DistributionLog");
            modelBuilder.Entity<Permission>().ToTable("Permission");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermission");
            modelBuilder.Entity<UserPermission>().ToTable("UserPermission");
            modelBuilder.Entity<StaffTask>().ToTable("StaffTask");
        }
    }
}