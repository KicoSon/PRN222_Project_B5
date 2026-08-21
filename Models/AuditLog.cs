using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class AuditLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public int? EntityId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? User { get; set; }
}
