using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class Bookmark
{
    public int BookmarkId { get; set; }

    public int StudentId { get; set; }

    public int JobId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Job Job { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
