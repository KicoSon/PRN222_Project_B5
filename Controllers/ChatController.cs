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
    private readonly IHubContext<ChatHub> _hub;

    public ChatController(
        StudentPartTimeJobDbContext context,
        IChatService chatService,
        INotificationService notificationService,
        IHubContext<ChatHub> hub)
    {
        _context = context;
        _chatService = chatService;
        _notificationService = notificationService;
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

        await _chatService.MarkAsReadAsync(id, userId);
        return View(room);
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatusInChat(int chatRoomId, int applicationId, string status)
    {
        var userId = GetCurrentUserId();
        var employerId = await _context.Employers
            .Where(e => e.UserId == userId)
            .Select(e => e.EmployerId)
            .FirstOrDefaultAsync();
        if (employerId == 0) return Unauthorized();

        var validStatuses = new[] { "Interview", "Approved", "Rejected" };
        if (!validStatuses.Contains(status)) return BadRequest(new { success = false, message = "Trạng thái không hợp lệ." });

        var application = await _context.Applications
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId
                && a.Job.EmployerId == employerId);
        if (application == null) return NotFound();

        var room = await _context.ChatRooms.FirstOrDefaultAsync(r => r.ChatRoomId == chatRoomId && r.ApplicationId == applicationId);
        if (room == null) return BadRequest(new { success = false, message = "Phòng chat không khớp đơn ứng tuyển." });

        application.Status = status;
        application.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(
            application.Student.UserId,
            "Cập nhật trạng thái ứng tuyển",
            $"Đơn ứng tuyển '{application.Job.Title}' đã chuyển sang: {status}.",
            "Application");

        var systemMessage = await _chatService.SendSystemMessageAsync(
            chatRoomId,
            $"Trạng thái hồ sơ đã được cập nhật: {GetStatusText(status)}.");

        await _hub.Clients.Group($"room-{chatRoomId}").SendAsync("ReceiveMessage", new
        {
            chatMessageId = systemMessage.ChatMessageId,
            senderUserId = (int?)null,
            content = systemMessage.Content,
            isSystemMessage = true,
            isFlagged = false,
            createdAt = systemMessage.CreatedAt.ToString("HH:mm")
        });

        await _hub.Clients.Group($"room-{chatRoomId}").SendAsync("ApplicationStatusChanged", new { status });
        return Json(new { success = true, status });
    }

    private static string GetStatusText(string status) => status switch
    {
        "Interview" => "Mời phỏng vấn",
        "Approved" => "Đã nhận",
        "Rejected" => "Từ chối",
        _ => status
    };

    private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
