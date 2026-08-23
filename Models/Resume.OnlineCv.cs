using System.Collections.Generic;

namespace StudentPartTime.Models;

// =====================================================================
// FEATURE: ONLINE-CV
// Partial extension of the existing Resume model. Adds the columns that
// were added to the dbo.Resumes table by OnlineCvFeature.sql (all NULL
// or have a DEFAULT, so existing file-upload resumes are unaffected).
// =====================================================================
public partial class Resume
{
    /// <summary>'File' (legacy uploaded file) | 'Online' (new online CV).</summary>
    public string ResumeType { get; set; } = "File";

    /// <summary>Desired job title shown on the online CV.</summary>
    public string? DesiredTitle { get; set; }

    /// <summary>Career objective / mục tiêu nghề nghiệp.</summary>
    public string? CareerObjective { get; set; }

    /// <summary>Education / học vấn.</summary>
    public string? Education { get; set; }

    /// <summary>Work experience / kinh nghiệm làm việc.</summary>
    public string? WorkExperience { get; set; }

    /// <summary>Projects / dự án đã làm.</summary>
    public string? Projects { get; set; }

    /// <summary>Certifications / chứng chỉ &amp; giải thưởng.</summary>
    public string? Certifications { get; set; }

    public virtual ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();
}