// src/CampusBites.Web/Services/SignalRDashboardNotifier.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.DTOs;
using CampusBites.Web.Hubs; // Need reference to the Hub
using Microsoft.AspNetCore.SignalR; // Need IHubContext
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CampusBites.Web.Services;

public class SignalRDashboardNotifier : IDashboardNotifier
{
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly ILogger<SignalRDashboardNotifier> _logger;

    public SignalRDashboardNotifier(IHubContext<DashboardHub> hubContext, ILogger<SignalRDashboardNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewOrderAsync(OrderSummaryDto orderSummary)
    {
        if (orderSummary == null) return;

        _logger.LogInformation("Notifying clients of new order: {OrderId}", orderSummary.Id);
        try
        {
            // Send message to ALL connected clients on the "ReceiveNewOrder" channel
            // Pass the orderSummary object as data
            await _hubContext.Clients.All.SendAsync("ReceiveNewOrder", orderSummary);
            // Later, you might send only to Admins: await _hubContext.Clients.Group("Admins")...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SignalR notification for new order {OrderId}", orderSummary.Id);
            // Don't let notification failure break the core operation
        }
    }
}