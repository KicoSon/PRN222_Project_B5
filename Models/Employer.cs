using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Employer
{
    public int EmployerId { get; set; }

    public int UserId { get; set; }

    public int CompanyId { get; set; }

    public string? Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual User User { get; set; } = null!;
}
