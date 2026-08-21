using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class JobType
{
    public int JobTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}
