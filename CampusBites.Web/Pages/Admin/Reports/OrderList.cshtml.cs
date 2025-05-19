// src/CampusBites.Web/Pages/Admin/Reports/OrderList.cshtml.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Security;
using CampusBites.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Inject UserManager to get User details
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel; // <-- Add ClosedXML using
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using CampusBites.Web.Reporting;
using QuestPDF.Fluent; // <-- Add IO using
using CampusBites.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using CampusBites.Infrastructure.Identity; // <-- Add using for OrderStatus



namespace CampusBites.Web.Pages.Admin.Reports;

[Authorize(Policy = Permissions.Orders.ViewAll)] // Use appropriate policy
public class OrderListModel : PageModel
{
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager; // To display user email

    // --- ADD Properties for Status Update ---
    [BindProperty]
    public int OrderIdToUpdate { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select a new status.")]
    public OrderStatus NewStatus { get; set; }
    // --- END Properties ---

    // --- ADD Filter Properties ---
    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? StartDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTimeOffset? EndDate { get; set; }
    // --- END Filter Properties ---

    [TempData] public string? Message { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public List<OrderSummaryDto> Orders { get; set; } = new();
    // Store user emails mapped by UserId for efficient lookup in the view
    public Dictionary<string, string?> UserEmails { get; set; } = new();

    public OrderListModel(IOrderService orderService, UserManager<ApplicationUser> userManager)
    {
        _orderService = orderService;
        _userManager = userManager;
    }

    public async Task OnGetAsync() // Add filtering parameters later (DateTime? startDate, etc.)
    {
        var ordersResult = await _orderService.GetAllOrderSummariesAsync(StartDate, EndDate);
        Orders = ordersResult?.ToList() ?? new List<OrderSummaryDto>();

        // Fetch user emails efficiently if there are orders
        // Optional: Fetch user emails for display if needed
        if (Orders.Any())
        {
            var userIds = Orders.Select(o => o.UserId).Distinct().ToList();
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            UserEmails = users.ToDictionary(u => u.Id, u => u.Email);
        }
    }

    // --- NEW EXPORT HANDLER ---
    // --- RENAMED Export Handler to OnGet... & Added date params ---
    public async Task<IActionResult> OnGetExportExcelAsync(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        // Use passed-in filter dates
        var orders = await _orderService.GetAllOrderSummariesAsync(startDate, endDate);
        var ordersList = orders?.ToList() ?? new List<OrderSummaryDto>();

        // Fetch user emails for the filtered orders
        var userEmails = new Dictionary<string, string?>();
        if (ordersList.Any())
        {
            var userIds = ordersList.Select(o => o.UserId).Distinct().ToList();
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            userEmails = users.ToDictionary(u => u.Id, u => u.Email);
        }

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Orders");
            var currentRow = 1;
            // Headers
            worksheet.Cell(currentRow, 1).Value = "Order ID";
            worksheet.Cell(currentRow, 2).Value = "Order Date";
            worksheet.Cell(currentRow, 3).Value = "User Email";
            worksheet.Cell(currentRow, 4).Value = "Total";
            worksheet.Cell(currentRow, 5).Value = "Status";
            worksheet.Cell(currentRow, 6).Value = "# Items";
            worksheet.Cell(currentRow, 7).Value = "Payment Method";
            worksheet.Cell(currentRow, 8).Value = "Payment Ref";
            worksheet.Row(currentRow).Style.Font.Bold = true;

            // Data
            foreach (var order in ordersList)
            {
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = order.Id;
                worksheet.Cell(currentRow, 2).Value = order.OrderDate.LocalDateTime; // Use LocalDateTime for Excel
                worksheet.Cell(currentRow, 3).Value = userEmails.TryGetValue(order.UserId, out var email) ? email : "N/A";
                worksheet.Cell(currentRow, 4).Value = order.OrderTotal;
                worksheet.Cell(currentRow, 5).Value = order.Status.ToString();
                worksheet.Cell(currentRow, 6).Value = order.NumberOfItems;
                worksheet.Cell(currentRow, 7).Value = order.PaymentMethod;
                worksheet.Cell(currentRow, 8).Value = order.PaymentReference;

                worksheet.Cell(currentRow, 2).Style.NumberFormat.Format = "yyyy-mm-dd hh:mm";
                worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "$#,##0.00";
            }
            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = $"CampusBites_Orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                return File(content, contentType, fileName);
            }
        }
    }
    // --- END EXPORT HANDLER ---

    // --- NEW PDF EXPORT HANDLER ---
    public async Task<IActionResult> OnGetExportPdfAsync(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        var orders = await _orderService.GetAllOrderSummariesAsync(startDate, endDate);
        var ordersList = orders?.ToList() ?? new List<OrderSummaryDto>();

        // Fetch user emails again for the PDF content
        var userEmails = new Dictionary<string, string?>();
        if (ordersList.Any())
        {
            var userIds = ordersList.Select(o => o.UserId).Distinct().ToList();
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            userEmails = users.ToDictionary(u => u.Id, u => u.Email);
        }

        try
        {
            // Create the QuestPDF document instance
            var document = new OrdersReportDocument(ordersList, userEmails, startDate, endDate);

            // Generate the PDF bytes
            // GeneratePdf() is simpler than stream for direct return
            byte[] pdfBytes = document.GeneratePdf();

            var fileName = $"CampusBites_Orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            // Log the error ex
            ErrorMessage = $"Failed to generate PDF report: {ex.Message}";
            return RedirectToPage(); // Redirect back with error message
        }
    }
    // --- END PDF EXPORT HANDLER ---

    // --- NEW: Handler for Updating Order Status ---
    public async Task<IActionResult> OnPostUpdateStatusAsync()
    {
        // Check if the correct policy is met (alternative to attribute)
        var authResult = await HttpContext.RequestServices.GetRequiredService<IAuthorizationService>()
                             .AuthorizeAsync(User, Permissions.Orders.EditStatus);
        if (!authResult.Succeeded)
        {
            return Forbid(); // Or return RedirectToPage with error
        }


        // Basic validation (OrderIdToUpdate should be > 0 implicitly)
        if (!ModelState.IsValid) // Checks if NewStatus was selected
        {
            // Need to reload data if returning the page with errors
            await OnGetAsync(); // Reload orders using existing filters
            return Page();
        }

        try
        {
            bool success = await _orderService.UpdateOrderStatusAsync(OrderIdToUpdate, NewStatus);

            if (success)
            {
                Message = $"Status for Order #{OrderIdToUpdate} updated successfully to {NewStatus}.";
                // Note: User notification email is sent within the service method now
            }
            else
            {
                ErrorMessage = $"Failed to update status for Order #{OrderIdToUpdate}. Order might not exist.";
                // Consider returning NotFound() if service indicates failure due to not found?
            }
        }
        catch (Exception ex)
        {
            // Log ex
            ErrorMessage = $"An error occurred while updating status for Order #{OrderIdToUpdate}: {ex.Message}";
        }

        // Redirect back to the same page (preserves filters if they are in query string)
        return RedirectToPage(new { StartDate = StartDate, EndDate = EndDate });
    }
    // --- END NEW HANDLER ---
}