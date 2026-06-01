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
        // CORE SYSTEM TABLES
        // =====================
        public DbSet<Role> Role { get; set; }
        public DbSet<UserAccount> UserAccount { get; set; }
        public DbSet<Participant> Participant { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Registration> Registration { get; set; }
        public DbSet<Guest> Guest { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<QrPass> QrPass { get; set; }

        // =====================
        // DISTRIBUTION SYSTEM
        // =====================
        public DbSet<DistributionItem> DistributionItem { get; set; }
        public DbSet<DistributionTask> DistributionTask { get; set; }
        public DbSet<DistributionLog> DistributionLog { get; set; }
        public DbSet<StaffTask> StaffTask { get; set; }
        public DbSet<OperationActivityLog> OperationActivityLog { get; set; }

        // =====================
        // PERMISSION SYSTEM
        // =====================
        public DbSet<Permission> Permission { get; set; }
        public DbSet<RolePermission> RolePermission { get; set; }
        public DbSet<UserPermission> UserPermission { get; set; }

        // =====================
        // OPTIONAL MODULES
        // =====================
        public DbSet<FoodMenu> FoodMenu { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================
            // TABLE MAPPING
            // =====================
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<Permission>().ToTable("Permission");
            modelBuilder.Entity<RolePermission>().ToTable("RolePermission");
            modelBuilder.Entity<UserPermission>().ToTable("UserPermission");

            // =====================
            // USER → ROLE
            // =====================
            modelBuilder.Entity<Registration>()
                .HasOne(r => r.QrPass)
                .WithOne(q => q.Registration)
                .HasForeignKey<QrPass>(q => q.RegistrationId);

            modelBuilder.Entity<UserAccount>()
                .HasOne(u => u.Role)
                .WithMany(r => r.UserAccounts)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================
            // ROLE PERMISSION (MANY TO MANY)
            // =====================
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

            // =====================
            // USER PERMISSION
            // =====================
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

            // =====================
            // DISTRIBUTION LOG RELATIONS
            // =====================
            modelBuilder.Entity<DistributionLog>()
                .HasOne(d => d.Registration)
                .WithMany()
                .HasForeignKey(d => d.RegistrationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DistributionLog>()
                .HasOne(d => d.UserAccount)
                .WithMany()
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

            modelBuilder.Entity<DistributionLog>()
                .HasIndex(x => new { x.RegistrationId, x.ActionType })
                .IsUnique();

            modelBuilder.Entity<DistributionLog>()
               .HasOne(d => d.DistributionTask)
               .WithMany()
               .HasForeignKey(d => d.DistributionTaskId)
               .OnDelete(DeleteBehavior.Restrict);

            // =====================
            // DISTRIBUTION TASK RELATIONS
            // =====================
            modelBuilder.Entity<DistributionTask>()
                .HasOne(t => t.Event)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StaffTask>()
                .HasOne(x => x.DistributionTask)
                .WithMany()
                .HasForeignKey(x => x.DistributionTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StaffTask>()
                .HasOne(x => x.UserAccount)
                .WithMany()
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QrPass>()
    .HasOne(x => x.Registration)
    .WithOne(x => x.QrPass)
    .HasForeignKey<QrPass>(x => x.RegistrationId);

            // =====================
            // SEED ROLES
            // =====================
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "admin" },
                new Role { RoleId = 2, RoleName = " eventmanager" },
                new Role { RoleId = 3, RoleName = "staff" },
                new Role { RoleId = 4, RoleName = "student" }
            );

            // =====================
            // SEED USERS
            // =====================
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
          UserAccountId = 2,
          FullName = "Event Manager",
          NickName = "eventmanager",
          Email = "eventmanager@gmail.com",
          Phone = "01711111111",
          PasswordHash = "t5Y6r3iXw1hT4Q3Y7e7nNWh0VvM6nZk+3+8v2/2+J1Y=",
          RoleId = 2,
          IsActive = true,
          CreatedAt = DateTime.Parse("2026-05-20 10:00:00")
      },

       new UserAccount
       {
           UserAccountId = 3,
           FullName = "Staff1",
           NickName = "staff1",
           Email = "staff1@gmail.com",
           Phone = "01722222222",
           PasswordHash = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
           RoleId = 3,
           IsActive = true,
           CreatedAt = DateTime.Parse("2026-05-20 10:00:00")
       },

      new UserAccount
      {
          UserAccountId = 4,
          FullName = "Staff2",
          NickName = "staff2",
          Email = "staff2@gmail.com",
          Phone = "01733333333",
          PasswordHash = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
          RoleId = 3,
          IsActive = true,
          CreatedAt = DateTime.Parse("2026-05-20 10:00:00")
      },

      new UserAccount
      {
          UserAccountId = 5,
          FullName = "Staff3",
          NickName = "staff3",
          Email = "staff3@gmail.com",
          Phone = "01744444444",
          PasswordHash = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
          RoleId = 3,
          IsActive = true,
          CreatedAt = DateTime.Parse("2026-05-20 10:00:00")
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
      }

      

     
  );
        }
    }
}