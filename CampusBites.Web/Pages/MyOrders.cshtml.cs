// src/CampusBites.Web/Pages/MyOrders.cshtml.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.DTOs; // For OrderSummaryDto
using CampusBites.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusBites.Web.Pages;

[Authorize] // Only logged-in users can see their orders
public class MyOrdersModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public List<OrderSummaryDto> Orders { get; set; } = new List<OrderSummaryDto>();

    [TempData]
    public string? Message { get; set; }

    public MyOrdersModel(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // This shouldn't happen if [Authorize] is working correctly
            return Challenge(); // Force login or show access denied
        }

        var ordersResult = await _orderService.GetUserOrdersAsync(user.Id);
        Orders = ordersResult?.ToList() ?? new List<OrderSummaryDto>(); // Handle potential null and convert IEnumerable to List

        return Page();
    }
}