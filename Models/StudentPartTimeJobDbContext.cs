using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace StudentPartTime.Models;

public partial class StudentPartTimeJobDbContext : DbContext
{
    public StudentPartTimeJobDbContext()
    {
    }

    public StudentPartTimeJobDbContext(DbContextOptions<StudentPartTimeJobDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Application> Applications { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Bookmark> Bookmarks { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Employer> Employers { get; set; }

    public virtual DbSet<Job> Jobs { get; set; }

    public virtual DbSet<JobType> JobTypes { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<Resume> Resumes { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Name=ConnectionStrings:MyCnn");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasIndex(e => e.JobId, "IX_Applications_JobId");

            entity.HasIndex(e => e.Status, "IX_Applications_Status");

            entity.HasIndex(e => e.StudentId, "IX_Applications_StudentId");

            entity.HasIndex(e => new { e.StudentId, e.JobId }, "UQ_Applications_Student_Job").IsUnique();

            entity.Property(e => e.AppliedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Job).WithMany(p => p.Applications)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Jobs");

            entity.HasOne(d => d.Resume).WithMany(p => p.Applications)
                .HasForeignKey(d => d.ResumeId)
                .HasConstraintName("FK_Applications_Resumes");

            entity.HasOne(d => d.Student).WithMany(p => p.Applications)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Applications_Students");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.HasIndex(e => new { e.EntityName, e.EntityId }, "IX_AuditLogs_Entity");

            entity.HasIndex(e => e.UserId, "IX_AuditLogs_UserId");

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.EntityName).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasIndex(e => e.JobId, "IX_Bookmarks_JobId");

            entity.HasIndex(e => e.StudentId, "IX_Bookmarks_StudentId");

            entity.HasIndex(e => new { e.StudentId, e.JobId }, "UQ_Bookmarks_Student_Job").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Job).WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookmarks_Jobs");

            entity.HasOne(d => d.Student).WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookmarks_Students");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_Categories_CategoryName").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(e => e.ProvinceId, "IX_Companies_ProvinceId");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.Website).HasMaxLength(255);

            entity.HasOne(d => d.Province).WithMany(p => p.Companies)
                .HasForeignKey(d => d.ProvinceId)
                .HasConstraintName("FK_Companies_Provinces");
        });

        modelBuilder.Entity<Employer>(entity =>
        {
            entity.HasIndex(e => e.CompanyId, "IX_Employers_CompanyId");

            entity.HasIndex(e => e.UserId, "IX_Employers_UserId");

            entity.HasIndex(e => e.UserId, "UQ_Employers_UserId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Position).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.Company).WithMany(p => p.Employers)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employers_Companies");

            entity.HasOne(d => d.User).WithOne(p => p.Employer)
                .HasForeignKey<Employer>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Employers_Users");
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasIndex(e => e.CategoryId, "IX_Jobs_CategoryId");

            entity.HasIndex(e => e.Deadline, "IX_Jobs_Deadline");

            entity.HasIndex(e => e.EmployerId, "IX_Jobs_EmployerId");

            entity.HasIndex(e => e.JobTypeId, "IX_Jobs_JobTypeId");

            entity.HasIndex(e => e.ProvinceId, "IX_Jobs_ProvinceId");

            entity.HasIndex(e => new { e.CategoryId, e.JobTypeId, e.ProvinceId, e.Status }, "IX_Jobs_Search");

            entity.HasIndex(e => e.Status, "IX_Jobs_Status");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.ApprovedAt).HasPrecision(0);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.RejectReason).HasMaxLength(500);
            entity.Property(e => e.SalaryMax).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SalaryMin).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
            entity.Property(e => e.WorkingTime).HasMaxLength(100);

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Jobs_ApprovedBy_Users");

            entity.HasOne(d => d.Category).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Categories");

            entity.HasOne(d => d.Employer).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.EmployerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Employers");

            entity.HasOne(d => d.JobType).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.JobTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_JobTypes");

            entity.HasOne(d => d.Province).WithMany(p => p.Jobs)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Jobs_Provinces");
        });

        modelBuilder.Entity<JobType>(entity =>
        {
            entity.HasIndex(e => e.TypeName, "UQ_JobTypes_TypeName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.TypeName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead }, "IX_Notifications_UserId_IsRead");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValue("System");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasIndex(e => e.ProvinceName, "UQ_Provinces_ProvinceName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.ProvinceName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);
        });

        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasIndex(e => e.StudentId, "IX_Resumes_StudentId");

            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.UploadedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Student).WithMany(p => p.Resumes)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Resumes_Students");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Students_UserId");

            entity.HasIndex(e => e.UserId, "UQ_Students_UserId").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Major).HasMaxLength(150);
            entity.Property(e => e.University).HasMaxLength(200);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasOne(d => d.User).WithOne(p => p.Student)
                .HasForeignKey<Student>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Users");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasPrecision(0);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRoles_Roles"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRoles_Users"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_UserRoles_RoleId");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
