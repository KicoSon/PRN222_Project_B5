using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Resume
{
    public int ResumeId { get; set; }

    public int StudentId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string? ContentType { get; set; }

    public long? FileSize { get; set; }

    public bool IsDefault { get; set; }

    public DateTime UploadedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual Student Student { get; set; } = null!;
}
