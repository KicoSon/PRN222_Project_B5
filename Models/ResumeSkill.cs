using System.Collections.Generic;

namespace StudentPartTime.Models;

public class ResumeSkill
{
    public int ResumeId { get; set; }

    public int SkillId { get; set; }

    public virtual Resume Resume { get; set; } = null!;

    public virtual Skill Skill { get; set; } = null!;
}