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

        public DbSet<Role> Roles { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Guest> Guests { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<QrPass> QrPasses { get; set; }
        public DbSet<DistributionLog> DistributionLogs { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<UserAccount>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.StudentId)
                .IsUnique();

            modelBuilder.Entity<Registration>()
                .HasIndex(r => new { r.ParticipantId, r.EventId })
                .IsUnique();

            modelBuilder.Entity<QrPass>()
                .HasIndex(q => q.RegistrationId)
                .IsUnique();

            modelBuilder.Entity<QrPass>()
                .HasIndex(q => q.QrCodeText)
                .IsUnique();

            modelBuilder.Entity<DistributionLog>()
              .HasOne(d => d.Participant)
              .WithMany(p => p.DistributionLogs)
              .HasForeignKey(d => d.ParticipantId)
              .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.PermissionName)
                .IsUnique();

            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique();

            modelBuilder.Entity<UserPermission>()
                .HasIndex(up => new { up.UserAccountId, up.PermissionId })
                .IsUnique();

            modelBuilder.Entity<UserAccount>()
                .HasOne(u => u.Participant)
                .WithOne(p => p.UserAccount)
                .HasForeignKey<Participant>(p => p.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Event>()
                .Property(e => e.BaseFee)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Event>()
                .Property(e => e.GuestFee)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Registration>()
                .Property(r => r.TotalAmount)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaidAmount)
                .HasColumnType("decimal(10,2)");
        }
    }
}