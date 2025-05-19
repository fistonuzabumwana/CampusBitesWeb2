// src/CampusBites.Web/Pages/Admin/MenuItems/Index.cshtml.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Security;
using CampusBites.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Required for TempData
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
// TODO: Add later: using Microsoft.AspNetCore.Authorization;

namespace CampusBites.Web.Pages.Admin.MenuItems;

[Authorize(Policy = Permissions.MenuItems.View)] // <--- UPDATE THIS ATTRIBUTE
public class IndexModel : PageModel
{
    private readonly IMenuItemService _menuItemService;

    public IList<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();

    [TempData] // Used to show success/error messages after redirects
    public string? Message { get; set; }

    public IndexModel(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    public async Task OnGetAsync()
    {
        // MenuItems = (List<MenuItemDto>)await _menuItemService.GetAllMenuItemsAsync();
        var serviceResult = await _menuItemService.GetAllMenuItemsAsync();

        // Convert the IEnumerable<MenuItemDto> result to a List<MenuItemDto>
        MenuItems = serviceResult.ToList();
    }

    // Handler for deleting an item
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            await _menuItemService.DeleteMenuItemAsync(id);
            Message = "Menu item deleted successfully.";
        }
        catch (KeyNotFoundException ex)
        {
            Message = $"Error: {ex.Message}";
            // Optionally log the error
           // return NotFound(); // Or redirect back with error message
        }
        catch (Exception ex)
        {
            Message = $"An unexpected error occurred: {ex.Message}";
            // Optionally log the error
        }
        return RedirectToPage(); // Refresh the Index page
    }
}