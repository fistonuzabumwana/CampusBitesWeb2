using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Security;
using CampusBites.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Web.Pages.Admin.MenuItems;

[Authorize(Policy = Permissions.MenuItems.Edit)]
public class EditModel : PageModel
{
    private readonly IMenuItemService _menuItemService;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public UpdateMenuItemDto MenuItem { get; set; } = default!;

    public string? CurrentImageUrl { get; set; }

    [TempData]
    public string? Message { get; set; }

    public EditModel(IMenuItemService menuItemService, ILogger<EditModel> logger)
    {
        _menuItemService = menuItemService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var menuItemDto = await _menuItemService.GetMenuItemByIdAsync(id.Value);
        if (menuItemDto == null) return NotFound();

        MenuItem = new UpdateMenuItemDto
        {
            Id = menuItemDto.Id,
            Name = menuItemDto.Name,
            Description = menuItemDto.Description,
            Price = menuItemDto.Price,
            Category = menuItemDto.Category,
            IsAvailable = menuItemDto.IsAvailable
        };

        this.CurrentImageUrl = menuItemDto.ImageUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate image file if provided
        if (MenuItem.ImageFile != null && MenuItem.ImageFile.Length > 0)
        {
            // Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var fileExtension = Path.GetExtension(MenuItem.ImageFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("MenuItem.ImageFile", "Only JPG, PNG, GIF, or WebP images are allowed");
                return Page();
            }

            // Validate file size (5MB max)
            if (MenuItem.ImageFile.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("MenuItem.ImageFile", "Image size must be less than 5MB");
                return Page();
            }

            try
            {
                // Validate image content
                using (var image = Image.Load(MenuItem.ImageFile.OpenReadStream()))
                {
                    // Image is valid
                }
            }
            catch
            {
                ModelState.AddModelError("MenuItem.ImageFile", "The uploaded file is not a valid image");
                return Page();
            }
        }

        try
        {
            await _menuItemService.UpdateMenuItemAsync(MenuItem.Id, MenuItem);
            Message = $"Menu item '{MenuItem.Name}' updated successfully.";
            return RedirectToPage("./Index");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Menu item not found during update");
            Message = "Error: The menu item could not be found";
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Argument error during menu item update");
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu item");
            ModelState.AddModelError(string.Empty, "An error occurred while updating the menu item. Please try again.");
            return Page();
        }
    }
}