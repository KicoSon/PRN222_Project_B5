using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Company
{
    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? Website { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Description { get; set; }

    public string? Address { get; set; }

    public int? ProvinceId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Employer> Employers { get; set; } = new List<Employer>();

    public virtual Province? Province { get; set; }
}
