using Microsoft.AspNetCore.SignalR;
using ServicePlatform.Models;
using ServicePlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace ServicePlatform.Hubs;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task JoinGroup(string requestId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, requestId);
    }

    public async Task SendMessage(string requestId, string senderName, string message)
    {
        // Broadcast to the specific request group
        await Clients.Group(requestId).SendAsync("ReceiveMessage", senderName, message, DateTime.UtcNow.ToString("HH:mm"));
    }
}
