// src/CampusBites.Web/Pages/OrderConfirmation.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Required for BindProperty query param
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusBites.Web.Pages;

[Authorize] // User must be logged in to see confirmation
public class OrderConfirmationModel : PageModel
{
    // Bind the orderId from the query string "?orderId=..."
    [BindProperty(SupportsGet = true)]
    public int OrderId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // To display the payment method used
    public Decimal TotalAmount { get; set; } // To display the total amount paid

    public string? ConfirmationMessage { get; set; }

    public IActionResult OnGet()
    {
        if (OrderId <= 0)
        {
            // Optional: Redirect if OrderId is missing or invalid
            // return RedirectToPage("/Index");
            ConfirmationMessage = "Thank you for your order!"; // Generic message if ID is missing
        }
        else
        {
            ConfirmationMessage = $"Thank you for your order! Your Order ID is: {OrderId}";
        }
        // TODO: Optionally fetch order details using OrderId to display more info
        return Page();
    }
}