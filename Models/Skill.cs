using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public class Skill
{
    public int SkillId { get; set; }

    public string SkillName { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();

    public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}