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

    // --- Chống lạm dụng / quấy rối (tái dùng bảng có sẵn, không thêm bảng mới) ---
    Task<bool> IsRoomClosedAsync(int chatRoomId);
    Task<bool> IsUserActiveAsync(int userId);
    Task<ChatMessage?> GetMessageAsync(int chatMessageId);
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
            // Đếm số lần người này từng bị gắn cờ trong đúng phòng chat này (dùng COUNT trên
            // cột IsFlagged đã có sẵn, không cần thêm cột đếm riêng) để cảnh báo leo thang.
            var priorFlaggedCount = await _context.ChatMessages
                .CountAsync(m => m.ChatRoomId == chatRoomId && m.SenderUserId == senderUserId && m.IsFlagged);

            var warningText = priorFlaggedCount >= 2
                ? "Tài khoản này đã nhiều lần chia sẻ liên hệ ngoài hệ thống. Nếu bị làm phiền, hãy dùng nút Báo cáo trên tin nhắn để gửi cho quản trị viên."
                : "Tin nhắn có dấu hiệu chia sẻ số điện thoại hoặc liên hệ ngoài hệ thống. Vì an toàn, vui lòng trao đổi công việc trong khung chat này.";

            warning = new ChatMessage
            {
                ChatRoomId = chatRoomId,
                SenderUserId = null,
                IsSystemMessage = true,
                IsFlagged = true,
                Content = warningText,
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
        var room = await _context.ChatRooms
            .Include(r => r.Student)
            .FirstAsync(r => r.ChatRoomId == chatRoomId);
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

    // Phòng chat tự khóa gửi tin khi hồ sơ đã bị Rejected - tái dùng Application.Status có sẵn,
    // không cần thêm cột "IsLocked" riêng cho ChatRoom.
    public Task<bool> IsRoomClosedAsync(int chatRoomId) =>
        _context.ChatRooms
            .Where(r => r.ChatRoomId == chatRoomId)
            .Select(r => r.Application.Status == "Rejected")
            .FirstOrDefaultAsync();

    // Nếu Admin đã khóa tài khoản (User.IsActive = false, chức năng Admin > Users có sẵn),
    // chặn luôn việc gửi tin ở tầng chat.
    public Task<bool> IsUserActiveAsync(int userId) =>
        _context.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.IsActive)
            .FirstOrDefaultAsync();

    public Task<ChatMessage?> GetMessageAsync(int chatMessageId) =>
        _context.ChatMessages.FirstOrDefaultAsync(m => m.ChatMessageId == chatMessageId);
}
