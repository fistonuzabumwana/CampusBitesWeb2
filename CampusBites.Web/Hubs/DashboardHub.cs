// src/CampusBites.Web/Hubs/DashboardHub.cs
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CampusBites.Web.Hubs;

// Hubs should generally be secured, but start without for simplicity
// TODO: Add authorization later if needed: [Authorize(Roles = "Admin")]
public class DashboardHub : Hub
{
    // This method can be called by connected clients (not needed for our scenario yet)
    // public async Task SendMessage(string user, string message)
    // {
    //     await Clients.All.SendAsync("ReceiveMessage", user, message);
    // }

    public override Task OnConnectedAsync()
    {
        // Optional: Logic when a client connects (e.g., add to group)
        // Consider logging: _logger.LogInformation("Dashboard client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // Optional: Logic when a client disconnects
        // Consider logging: _logger.LogInformation("Dashboard client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}