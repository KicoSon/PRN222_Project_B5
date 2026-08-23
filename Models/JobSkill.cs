using System.Collections.Generic;

namespace StudentPartTime.Models;

public class JobSkill
{
    public int JobId { get; set; }

    public int SkillId { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}