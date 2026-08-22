using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StudentPartTime.Services;

namespace StudentPartTime.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService) => _chatService = chatService;

    public async Task JoinRoom(int chatRoomId)
    {
        var userId = GetUserId();
        if (!await _chatService.IsRoomMemberAsync(chatRoomId, userId))
            throw new HubException("Bạn không có quyền truy cập cuộc trò chuyện này.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(chatRoomId));
    }

    public async Task SendMessage(int chatRoomId, string content)
    {
        var userId = GetUserId();
        if (!await _chatService.IsRoomMemberAsync(chatRoomId, userId))
            throw new HubException("Không có quyền gửi tin trong phòng này.");
        if (string.IsNullOrWhiteSpace(content)) return;
        if (content.Length > 1000) throw new HubException("Tin nhắn tối đa 1000 ký tự.");

        if (!await _chatService.IsUserActiveAsync(userId))
            throw new HubException("Tài khoản của bạn đã bị khóa và không thể gửi tin nhắn.");

        if (await _chatService.IsRoomClosedAsync(chatRoomId))
            throw new HubException("Cuộc trò chuyện đã đóng vì hồ sơ ứng tuyển đã bị từ chối.");

        var (message, warning) = await _chatService.SendMessageAsync(chatRoomId, userId, content);
        await Clients.Group(GroupName(chatRoomId)).SendAsync("ReceiveMessage", ToDto(message));

        if (warning != null)
            await Clients.Group(GroupName(chatRoomId)).SendAsync("ReceiveMessage", ToDto(warning));
    }

    private static object ToDto(StudentPartTime.Models.ChatMessage message) => new
    {
        chatMessageId = message.ChatMessageId,
        senderUserId = message.SenderUserId,
        content = message.Content,
        isSystemMessage = message.IsSystemMessage,
        isFlagged = message.IsFlagged,
        createdAt = message.CreatedAt.ToString("HH:mm")
    };

    private static string GroupName(int roomId) => $"room-{roomId}";
    private int GetUserId() => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
