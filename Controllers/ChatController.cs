using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentPartTime.Hubs;
using StudentPartTime.Models;
using StudentPartTime.Services;

namespace StudentPartTime.Controllers;

[Authorize(Roles = "Student,Employer")]
public class ChatController : Controller
{
    private readonly StudentPartTimeJobDbContext _context;
    private readonly IChatService _chatService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(
        StudentPartTimeJobDbContext context,
        IChatService chatService,
        INotificationService notificationService,
        IAuditService auditService,
        IHubContext<ChatHub> hub)
    {
        _context = context;
        _chatService = chatService;
        _notificationService = notificationService;
        _auditService = auditService;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var rooms = await _chatService.GetRoomsForUserAsync(userId);
        ViewBag.CurrentUserId = userId;
        ViewBag.IsEmployer = User.IsInRole("Employer");
        return View(rooms);
    }

    [HttpGet]
    public async Task<IActionResult> Open(int applicationId)
    {
        var userId = GetCurrentUserId();
        var application = await _context.Applications
            .Include(a => a.Job)
            .Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

        if (application == null) return NotFound();

        var allowed = User.IsInRole("Student")
            ? application.Student.UserId == userId
            : application.Job.EmployerId == (await _context.Employers.Where(e => e.UserId == userId).Select(e => e.EmployerId).FirstOrDefaultAsync());

        if (!allowed) return Forbid();

        var room = await _chatService.GetOrCreateRoomAsync(applicationId);
        return RedirectToAction(nameof(Room), new { id = room.ChatRoomId });
    }

    [HttpGet]
    public async Task<IActionResult> Room(int id)
    {
        var userId = GetCurrentUserId();
        if (!await _chatService.IsRoomMemberAsync(id, userId)) return Forbid();

        var room = await _context.ChatRooms
            .Include(r => r.Job).ThenInclude(j => j.Province)
            .Include(r => r.Job).ThenInclude(j => j.Employer).ThenInclude(e => e.Company)
            .Include(r => r.Student).ThenInclude(s => s.User)
            .Include(r => r.Student).ThenInclude(s => s.Resumes)
            .Include(r => r.Employer).ThenInclude(e => e.User)
            .Include(r => r.Employer).ThenInclude(e => e.Company)
            .FirstOrDefaultAsync(r => r.ChatRoomId == id);

        if (room == null) return NotFound();

        ViewBag.Messages = await _chatService.GetMessagesAsync(id);
        ViewBag.CurrentUserId = userId;
        ViewBag.Application = await _context.Applications
            .Include(a => a.Resume)
            .FirstOrDefaultAsync(a => a.ApplicationId == room.ApplicationId);
        ViewBag.IsEmployer = User.IsInRole("Employer");

        // Trạng thái khóa phòng: hồ sơ đã Rejected, hoặc phía đối diện đã bị Admin khóa tài khoản.
        ViewBag.RoomClosed = await _chatService.IsRoomClosedAsync(id);
        var otherUserId = User.IsInRole("Employer") ? room.Student.UserId : room.Employer.UserId;
        ViewBag.OtherUserActive = await _chatService.IsUserActiveAsync(otherUserId);

        await _chatService.MarkAsReadAsync(id, userId);
        return View(room);
    }

    // Báo cáo 1 tin nhắn quấy rối/spam cho quản trị viên - ghi vào AuditLogs có sẵn,
    // không tạo bảng "Report" riêng. Admin xem tại Admin > Nhật ký hệ thống, lọc theo
    // Hành động = "ReportChatMessage".
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReportMessage(int chatMessageId, int chatRoomId, string? reason)
    {
        var userId = GetCurrentUserId();
        if (!await _chatService.IsRoomMemberAsync(chatRoomId, userId))
            return Forbid();

        var message = await _chatService.GetMessageAsync(chatMessageId);
        if (message == null || message.ChatRoomId != chatRoomId)
            return NotFound();

        if (message.SenderUserId == userId)
            return BadRequest(new { success = false, message = "Không thể tự báo cáo tin nhắn của chính mình." });

        var snippet = message.Content.Length > 200 ? message.Content[..200] + "..." : message.Content;
        var description = $"Báo cáo tin nhắn #{chatMessageId} trong phòng #{chatRoomId} (người gửi: {message.SenderUserId}): \"{snippet}\"" +
            (string.IsNullOrWhiteSpace(reason) ? "" : $" — Lý do: {reason.Trim()}");

        await _auditService.LogActionAsync(userId, "ReportChatMessage", "ChatMessage", chatMessageId, description);

        return Json(new { success = true });
    }

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
