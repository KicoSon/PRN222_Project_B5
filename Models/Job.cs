using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Job
{
    public int JobId { get; set; }

    public int EmployerId { get; set; }

    public int CategoryId { get; set; }

    public int JobTypeId { get; set; }

    public int ProvinceId { get; set; }

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Requirement { get; set; }

    public string? Benefit { get; set; }

    public decimal? SalaryMin { get; set; }

    public decimal? SalaryMax { get; set; }

    public int Quantity { get; set; }

    public string? WorkingTime { get; set; }

    public string? Address { get; set; }

    public DateOnly Deadline { get; set; }

    public string Status { get; set; } = null!;

    public string? RejectReason { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public virtual Category Category { get; set; } = null!;

    public virtual Employer Employer { get; set; } = null!;

    public virtual JobType JobType { get; set; } = null!;

    public virtual Province Province { get; set; } = null!;
}
