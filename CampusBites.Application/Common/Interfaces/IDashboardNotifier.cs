// src/CampusBites.Application/Common/Interfaces/IDashboardNotifier.cs
using CampusBites.Application.DTOs; // For OrderSummaryDto
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IDashboardNotifier
{
    /// <summary>
    /// Sends a notification about a newly created order to relevant clients.
    /// </summary>
    /// <param name="orderSummary">Summary data of the new order.</param>
    Task NotifyNewOrderAsync(OrderSummaryDto orderSummary);

    // Add other notification methods later if needed
    // Task NotifyOrderStatusUpdateAsync(int orderId, string newStatus);
}