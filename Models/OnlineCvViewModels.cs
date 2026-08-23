using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentPartTime.Models;

// =====================================================================
// FEATURE: ONLINE-CV
// =====================================================================

/// <summary>
/// Binds the online CV form (Create / Edit). SkillIds is used to remember
/// the selection across validation round-trips.
/// </summary>
public class OnlineCvViewModel
{
    public int? ResumeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập vị trí mong muốn.")]
    [StringLength(150, ErrorMessage = "Vị trí mong muốn không quá 150 ký tự.")]
    [Display(Name = "Vị trí mong muốn")]
    public string DesiredTitle { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mục tiêu nghề nghiệp.")]
    [Display(Name = "Mục tiêu nghề nghiệp")]
    public string CareerObjective { get; set; } = null!;

    [Display(Name = "Học vấn")]
    public string? Education { get; set; }

    [Display(Name = "Kinh nghiệm làm việc")]
    public string? WorkExperience { get; set; }

    [Display(Name = "Dự án đã thực hiện")]
    public string? Projects { get; set; }

    [Display(Name = "Chứng chỉ / Giải thưởng")]
    public string? Certifications { get; set; }

    public List<int> SelectedSkillIds { get; set; } = new List<int>();
}

/// <summary>
/// A single recommended job produced by the matching algorithm, carrying
/// the computed percentage and the matched/missing skill names for display.
/// </summary>
public class JobRecommendationViewModel
{
    public int JobId { get; set; }
    public string Title { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? ProvinceName { get; set; }
    public string? JobTypeName { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public DateOnly Deadline { get; set; }

    public int MatchPercent { get; set; }
    public int MatchedCount { get; set; }
    public List<string> MatchedSkills { get; set; } = new List<string>();
    public List<string> MissingSkills { get; set; } = new List<string>();
}