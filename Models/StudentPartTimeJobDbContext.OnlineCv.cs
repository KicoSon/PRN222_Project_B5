using Microsoft.EntityFrameworkCore;

namespace StudentPartTime.Models;

// =====================================================================
// FEATURE: ONLINE-CV
// Partial extension of the existing DbContext. Adds DbSets for the 3 new
// tables created by OnlineCvFeature.sql (Skills / ResumeSkills / JobSkills)
// and the mapping for the columns added to Resumes via ALTER TABLE.
// The tables already exist in the DB - no migration is required.
// =====================================================================
public partial class StudentPartTimeJobDbContext
{
    public virtual DbSet<Skill> Skills { get; set; }

    public virtual DbSet<ResumeSkill> ResumeSkills { get; set; }

    public virtual DbSet<JobSkill> JobSkills { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Skills
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.SkillId);
            entity.HasIndex(e => e.SkillName, "UQ_Skills_SkillName").IsUnique();
            entity.Property(e => e.SkillName).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("sysdatetime()");
        });

        // ResumeSkills (composite PK)
        modelBuilder.Entity<ResumeSkill>(entity =>
        {
            entity.HasKey(e => new { e.ResumeId, e.SkillId });
            entity.HasIndex(e => e.SkillId, "IX_ResumeSkills_SkillId");

            entity.HasOne(d => d.Resume)
                .WithMany(p => p.ResumeSkills)
                .HasForeignKey(d => d.ResumeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ResumeSkills_Resumes");

            entity.HasOne(d => d.Skill)
                .WithMany(p => p.ResumeSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ResumeSkills_Skills");
        });

        // JobSkills (composite PK)
        modelBuilder.Entity<JobSkill>(entity =>
        {
            entity.HasKey(e => new { e.JobId, e.SkillId });
            entity.HasIndex(e => e.SkillId, "IX_JobSkills_SkillId");

            entity.HasOne(d => d.Job)
                .WithMany(p => p.JobSkills)
                .HasForeignKey(d => d.JobId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_JobSkills_Jobs");

            entity.HasOne(d => d.Skill)
                .WithMany(p => p.JobSkills)
                .HasForeignKey(d => d.SkillId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_JobSkills_Skills");
        });

        // New Resume columns added by ALTER TABLE
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.Property(e => e.ResumeType)
                .HasMaxLength(10)
                .HasDefaultValue("File");
            entity.Property(e => e.DesiredTitle).HasMaxLength(150);
            entity.HasIndex(e => e.ResumeType, "IX_Resumes_ResumeType");
        });
    }
}