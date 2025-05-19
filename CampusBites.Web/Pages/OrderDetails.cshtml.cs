// src/CampusBites.Web/Pages/OrderDetails.cshtml.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.DTOs; // For OrderDetailsDto
using CampusBites.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace CampusBites.Web.Pages;

[Authorize] // User must be logged in
public class OrderDetailsModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;

    // Property to hold the details for the view
    public OrderDetailsDto? Order { get; set; } // Nullable if order not found/allowed

    public OrderDetailsModel(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    // OnGetAsync receives the order ID from the route
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound("Order ID not provided.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge(); // Should not happen if [Authorize] works
        }

        // Call the service to get details, passing order ID and user ID for verification
        Order = await _orderService.GetOrderDetailsAsync(id.Value, user.Id);

        if (Order == null)
        {
            // Order service returns null if not found OR if user doesn't own it
            return NotFound("Order not found or you do not have permission to view it.");
        }

        return Page();
    }
}