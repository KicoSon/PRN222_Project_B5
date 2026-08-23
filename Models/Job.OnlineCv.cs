using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentPartTime.Models;

// =====================================================================
// FEATURE: ONLINE-CV
// Adds a [NotMapped] skill-id collection to Job purely for binding the
// checkbox list on the employer create/edit job form. It is NOT stored
// as a column. The real many-to-many rows live in the JobSkills table.
// =====================================================================
public partial class Job
{
    [NotMapped]
    public List<int> SkillIds { get; set; } = new List<int>();

    public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
}