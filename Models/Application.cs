using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Application
{
    public int ApplicationId { get; set; }

    public int StudentId { get; set; }

    public int JobId { get; set; }

    public int? ResumeId { get; set; }

    public string? CoverLetter { get; set; }

    public string Status { get; set; } = null!;

    public string? EmployerNote { get; set; }

    public DateTime AppliedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Resume? Resume { get; set; }

    public virtual Student Student { get; set; } = null!;
}
