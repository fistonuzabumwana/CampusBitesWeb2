using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Security;
using CampusBites.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SixLabors.ImageSharp;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Web.Pages.Admin.MenuItems;

[Authorize(Policy = Permissions.MenuItems.Create)]
public class CreateModel : PageModel
{
    private readonly IMenuItemService _menuItemService;
    private readonly ILogger<CreateModel> _logger;

    [BindProperty]
    public CreateMenuItemDto MenuItem { get; set; } = new CreateMenuItemDto();

    [TempData]
    public string? Message { get; set; }

    public CreateModel(IMenuItemService menuItemService, ILogger<CreateModel> logger)
    {
        _menuItemService = menuItemService;
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate image file
        if (MenuItem.ImageFile == null || MenuItem.ImageFile.Length == 0)
        {
            ModelState.AddModelError("MenuItem.ImageFile", "Please upload an image");
            return Page();
        }

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
            try
            {
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

            var createdItem = await _menuItemService.CreateMenuItemAsync(MenuItem);
            Message = $"Menu item '{createdItem.Name}' created successfully.";
            return RedirectToPage("./Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu item");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the menu item. Please try again.");
            return Page();
        }
    }
}