using System;
using System.Collections.Generic;

namespace StudentPartTime.Models;

public partial class ChatRoom
{
    public int ChatRoomId { get; set; }
    public int ApplicationId { get; set; }
    public int JobId { get; set; }
    public int StudentId { get; set; }
    public int EmployerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessageAt { get; set; }

    public virtual Application Application { get; set; } = null!;
    public virtual Job Job { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
    public virtual Employer Employer { get; set; } = null!;
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
