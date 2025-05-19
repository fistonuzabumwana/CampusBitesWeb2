// src/CampusBites.Web/Pages/Admin/Users/Index.cshtml.cs
using CampusBites.Application.Common.Security; // For Permissions
using CampusBites.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;      // For [Authorize]
using Microsoft.AspNetCore.Identity;           // For UserManager, ApplicationUser
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;           // For ToListAsync()
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CampusBites.Web.Pages.Admin.Users;

[Authorize(Policy = Permissions.Users.View)] // Secure page using the permission policy
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    // Property to hold the list of users for the view
    public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    // Inject SignInManager to check if the admin is trying to delete themselves
    private readonly SignInManager<ApplicationUser> _signInManager;


    // Properties for TempData messages
    [TempData]
    public string? SuccessMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager; // Initialize SignInManager

    }

    public async Task OnGetAsync()
    {
        // Retrieve all users from Identity store
        // Note: For large numbers of users, consider pagination
        Users = await _userManager.Users.ToListAsync();
    }

    [Authorize(Policy = Permissions.Users.Delete)] // Ensure only users with delete permission can execute
    public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            ErrorMessage = "User ID cannot be empty.";
            return RedirectToPage();
        }

        var userToDelete = await _userManager.FindByIdAsync(userId);

        if (userToDelete == null)
        {
            ErrorMessage = "User not found.";
            return RedirectToPage();
        }

        // Prevent an admin from deleting their own account
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userToDelete.Id == currentUserId)
        {
            ErrorMessage = "You cannot delete your own account.";
            return RedirectToPage();
        }

        // Consider if there are other super-admins or protected users you don't want to be deleted.
        // Example: if (await _userManager.IsInRoleAsync(userToDelete, "SuperAdmin")) { ... prevent ... }

        var result = await _userManager.DeleteAsync(userToDelete);

        if (result.Succeeded)
        {
            SuccessMessage = $"User '{userToDelete.UserName}' has been successfully deleted.";
        }
        else
        {
            ErrorMessage = $"Error deleting user '{userToDelete.UserName}'.";
            foreach (var error in result.Errors)
            {
                ErrorMessage += $" {error.Description}";
            }
        }

        return RedirectToPage(); // Refreshes the user list by re-running OnGetAsync
    }

    // Your existing LockUser and UnlockUser handlers would also go here
    // For example:
    // [Authorize(Policy = Permissions.Users.Edit)] // Or a specific Lock/Unlock permission
    // public async Task<IActionResult> OnPostLockUserAsync(string userId) { /* ... logic ... */ return RedirectToPage(); }
    // [Authorize(Policy = Permissions.Users.Edit)]
    // public async Task<IActionResult> OnPostUnlockUserAsync(string userId) { /* ... logic ... */ return RedirectToPage(); }

}