// src/CampusBites.Web/Pages/Admin/Dashboard/Index.cshtml.cs
using CampusBites.Application.Common.Interfaces; // Add
using CampusBites.Application.Common.Security;
using CampusBites.Application.DTOs; // Add for DTOs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System; // Add for DateTimeOffset
using System.Threading.Tasks; // Add for Task

namespace CampusBites.Web.Pages.Admin.Dashboard;

[Authorize(Policy = Permissions.Orders.ViewAll)] // Example policy
public class IndexModel : PageModel
{
    private readonly IOrderService _orderService; // Inject OrderService

    public SalesSummaryDto? TodaySummary { get; set; }
    public SalesSummaryDto? WeekSummary { get; set; }
    // Add more summaries if needed (Month, Year)

    public IndexModel(IOrderService orderService) // Update constructor
    {
        _orderService = orderService;
    }

    public async Task OnGetAsync()
    {
        var now = DateTimeOffset.UtcNow; // Use UTC consistently
        var todayStart = now.Date;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek); // Assuming Sunday as start of week

        TodaySummary = await _orderService.GetSalesSummaryAsync("Today", todayStart, todayStart); // End date is inclusive in display but exclusive in query, adjust repo logic if needed
        WeekSummary = await _orderService.GetSalesSummaryAsync("This Week", weekStart, todayStart); // From start of week up to today
    }
}