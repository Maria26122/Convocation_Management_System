using Convocation.Entities;
using Convocation.Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Convocation.DataAccess
{
    public class ConvocationDbContext : DbContext
    {
        public ConvocationDbContext(DbContextOptions<ConvocationDbContext> options)
            : base(options)
        {
        }

        // =====================
        // DBSets (SINGULAR)
        // =====================
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
        public DbSet<UserPermission> UserPermission { get; set; }
        public DbSet<StaffTask> StaffTask { get; set; }
        public DbSet<DistributionItem> DistributionItem { get; set; }
        public DbSet<FoodMenu> FoodMenu { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TABLES
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<Permission>().ToTable("Permission");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermission");
            modelBuilder.Entity<UserPermission>().ToTable("UserPermission");

            // ROLE → USER
            modelBuilder.Entity<UserAccount>()
                .HasOne(u => u.Role)
                .WithMany(r => r.UserAccounts)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ROLE PERMISSION (MANY TO MANY)
            modelBuilder.Entity<RolePermission>()
                .HasKey(x => new { x.RoleId, x.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(x => x.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(x => x.PermissionId);

            // USER PERMISSION
            modelBuilder.Entity<UserPermission>()
                .HasKey(x => x.UserPermissionId);

            modelBuilder.Entity<UserPermission>()
                .HasOne(x => x.UserAccount)
                .WithMany(u => u.UserPermissions)
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserPermission>()
                .HasOne(x => x.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DistributionLog>()
                .HasOne(d => d.Registration)
                .WithMany()
                .HasForeignKey(d => d.RegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DistributionLog>()
                .HasOne(d => d.UserAccount)
                .WithMany(u => u.DistributionLogs)
                .HasForeignKey(d => d.UserAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DistributionLog>()
                .HasOne(d => d.Event)
                .WithMany()
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DistributionLog>()
                .HasOne(d => d.Participant)
                .WithMany()
                .HasForeignKey(d => d.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            // SEED ROLES
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Event Manager" },
                new Role { RoleId = 3, RoleName = "Staff" },
                new Role { RoleId = 4, RoleName = "Student" }
            );
             


            // USER ACCOUNT SEEDING
            modelBuilder.Entity<UserAccount>().HasData(

                new UserAccount
                {
                    UserAccountId = 1,
                    FullName = "Administrator",
                    NickName = "admin",
                    Email = "admin@gmail.com",
                    Phone = "01700000000",
                    PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=",
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-17 19:18:41.7900000")
                },

                new UserAccount
                {
                    UserAccountId = 3,
                    FullName = "Maria Islam Shuchona",
                    NickName = "",
                    Email = "mariaislam1226@gmail.com",
                    Phone = "01851355381",
                    PasswordHash = "Ym48gF537rRyxCxr5ge+KvesXAj9cFDyeOAzD+gav1c=",
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-17 19:56:47.9599201")
                },

                new UserAccount
                {
                    UserAccountId = 4,
                    FullName = "Shuchona",
                    NickName = "",
                    Email = "shuchona@gmail.com",
                    Phone = "01851355382",
                    PasswordHash = "2IwvVDg6k/EX5OOg/KjtgFYefq1v3L4iuvS2E3iWC+k=",
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-17 20:37:24.6637944")
                },

                new UserAccount
                {
                    UserAccountId = 5,
                    FullName = "Maria",
                    NickName = null,
                    Email = "mis@gmail.com",
                    Phone = "01851355382",
                    PasswordHash = "wskrEOQJE84GQlOhnsUEpAzcJeQMqKF1eo4QhS3tEOw=",
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-18 00:35:42.6096320")
                },

                new UserAccount
                {
                    UserAccountId = 6,
                    FullName = "maria",
                    NickName = null,
                    Email = "maria@gmail.com",
                    Phone = "01851355382",
                    PasswordHash = "lK7J++2Yns4Ymn4XLJz0FmkFBJUVK8TB2/KjjX/YVic=",
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-18 12:07:03.8147314")
                },

                new UserAccount
                {
                    UserAccountId = 7,
                    FullName = "Shuchonaa",
                    NickName = null,
                    Email = "shuch@gmail.com",
                    Phone = "01851355381",
                    PasswordHash = "o89N5RAvRkvkkiLfq5xtreNIeAtDDvRCCllSH/npa2I=",
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-18 12:14:13.7167778")
                },

                new UserAccount
                {
                    UserAccountId = 9,
                    FullName = "ariyan",
                    NickName = null,
                    Email = "ary@gmail.com",
                    Phone = "01851355381",
                    PasswordHash = "DwGS1cbVgZdTJReuXUr1SCJ/+jlpRCsdt0iOKYxfyDk=",
                    RoleId = 4,
                    IsActive = true,
                    CreatedAt = DateTime.Parse("2026-05-18 14:42:29.5257312")
                }
            );

        }
    }
}