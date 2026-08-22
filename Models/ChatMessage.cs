using System;

namespace StudentPartTime.Models;

public partial class ChatMessage
{
    public int ChatMessageId { get; set; }
    public int ChatRoomId { get; set; }
    public int? SenderUserId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsSystemMessage { get; set; }
    public bool IsFlagged { get; set; }
    public bool IsReadByStudent { get; set; }
    public bool IsReadByEmployer { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ChatRoom ChatRoom { get; set; } = null!;
    public virtual User? Sender { get; set; }
}
