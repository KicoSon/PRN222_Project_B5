using Microsoft.EntityFrameworkCore;
using StudentPartTime.Models;

namespace StudentPartTime.Services;

public interface IChatService
{
    Task<ChatRoom> GetOrCreateRoomAsync(int applicationId);
    Task<bool> IsRoomMemberAsync(int chatRoomId, int userId);
    Task<List<ChatMessage>> GetMessagesAsync(int chatRoomId);
    Task<(ChatMessage Message, ChatMessage? Warning)> SendMessageAsync(int chatRoomId, int senderUserId, string content);
    Task<ChatMessage> SendSystemMessageAsync(int chatRoomId, string content, bool flagged = false);
    Task<List<ChatRoom>> GetRoomsForUserAsync(int userId);
    Task MarkAsReadAsync(int chatRoomId, int userId);
}

public class ChatService : IChatService
{
    private readonly StudentPartTimeJobDbContext _context;

    public ChatService(StudentPartTimeJobDbContext context) => _context = context;

    public async Task<ChatRoom> GetOrCreateRoomAsync(int applicationId)
    {
        var existing = await _context.ChatRooms
            .Include(r => r.Job).ThenInclude(j => j.Employer).ThenInclude(e => e.Company)
            .Include(r => r.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);
        if (existing != null) return existing;

        var application = await _context.Applications
            .Include(a => a.Job)
            .Include(a => a.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null)
            throw new InvalidOperationException("Application does not exist.");

        var room = new ChatRoom
        {
            ApplicationId = application.ApplicationId,
            JobId = application.JobId,
            StudentId = application.StudentId,
            EmployerId = application.Job.EmployerId,
            CreatedAt = DateTime.Now
        };

        _context.ChatRooms.Add(room);
        await _context.SaveChangesAsync();

        var greeting = new ChatMessage
        {
            ChatRoomId = room.ChatRoomId,
            SenderUserId = null,
            IsSystemMessage = true,
            Content = $"{application.Student.User.FullName} vừa ứng tuyển vị trí \"{application.Job.Title}\". Hãy trao đổi thêm về công việc tại đây!",
            CreatedAt = DateTime.Now
        };

        _context.ChatMessages.Add(greeting);
        room.LastMessageAt = greeting.CreatedAt;
        await _context.SaveChangesAsync();

        return room;
    }

    public Task<bool> IsRoomMemberAsync(int chatRoomId, int userId) =>
        _context.ChatRooms.AnyAsync(r => r.ChatRoomId == chatRoomId &&
            (r.Student.UserId == userId || r.Employer.UserId == userId));

    public Task<List<ChatMessage>> GetMessagesAsync(int chatRoomId) =>
        _context.ChatMessages
            .Where(m => m.ChatRoomId == chatRoomId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.ChatMessageId)
            .ToListAsync();

    public async Task<(ChatMessage Message, ChatMessage? Warning)> SendMessageAsync(int chatRoomId, int senderUserId, string content)
    {
        var message = new ChatMessage
        {
            ChatRoomId = chatRoomId,
            SenderUserId = senderUserId,
            Content = content.Trim(),
            IsFlagged = SpamDetector.IsSuspicious(content),
            CreatedAt = DateTime.Now
        };

        _context.ChatMessages.Add(message);
        ChatMessage? warning = null;

        if (message.IsFlagged)
        {
            warning = new ChatMessage
            {
                ChatRoomId = chatRoomId,
                SenderUserId = null,
                IsSystemMessage = true,
                IsFlagged = true,
                Content = "Tin nhắn có dấu hiệu chia sẻ số điện thoại hoặc liên hệ ngoài hệ thống. Vì an toàn, vui lòng trao đổi công việc trong khung chat này.",
                CreatedAt = DateTime.Now.AddMilliseconds(1)
            };
            _context.ChatMessages.Add(warning);
        }

        var room = await _context.ChatRooms.FirstAsync(r => r.ChatRoomId == chatRoomId);
        room.LastMessageAt = message.CreatedAt;
        await _context.SaveChangesAsync();
        return (message, warning);
    }

    public async Task<ChatMessage> SendSystemMessageAsync(int chatRoomId, string content, bool flagged = false)
    {
        var message = new ChatMessage
        {
            ChatRoomId = chatRoomId,
            SenderUserId = null,
            IsSystemMessage = true,
            IsFlagged = flagged,
            Content = content,
            CreatedAt = DateTime.Now
        };

        _context.ChatMessages.Add(message);
        var room = await _context.ChatRooms.FirstAsync(r => r.ChatRoomId == chatRoomId);
        room.LastMessageAt = message.CreatedAt;
        await _context.SaveChangesAsync();
        return message;
    }

    public Task<List<ChatRoom>> GetRoomsForUserAsync(int userId) =>
        _context.ChatRooms
            .Include(r => r.Job).ThenInclude(j => j.Employer).ThenInclude(e => e.Company)
            .Include(r => r.Student).ThenInclude(s => s.User)
            .Include(r => r.Employer).ThenInclude(e => e.User)
            .Include(r => r.Messages)
            .Where(r => r.Student.UserId == userId || r.Employer.UserId == userId)
            .OrderByDescending(r => r.LastMessageAt ?? r.CreatedAt)
            .ToListAsync();

    public async Task MarkAsReadAsync(int chatRoomId, int userId)
    {
        var room = await _context.ChatRooms.FirstAsync(r => r.ChatRoomId == chatRoomId);
        var isStudent = room.Student.UserId == userId;
        var messages = await _context.ChatMessages
            .Where(m => m.ChatRoomId == chatRoomId && !m.IsSystemMessage)
            .ToListAsync();

        foreach (var message in messages)
        {
            if (isStudent) message.IsReadByStudent = true;
            else message.IsReadByEmployer = true;
        }

        await _context.SaveChangesAsync();
    }
}
