using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int UserId { get; set; }

    public string? University { get; set; }

    public string? Major { get; set; }

    public int? GraduationYear { get; set; }

    public string? Experience { get; set; }

    public string? SkillSummary { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    public virtual ICollection<Resume> Resumes { get; set; } = new List<Resume>();

    public virtual User User { get; set; } = null!;
}
