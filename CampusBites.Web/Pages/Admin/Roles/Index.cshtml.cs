// src/CampusBites.Web/Pages/Admin/Roles/Index.cshtml.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Security; // For Permissions
using CampusBites.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // For ToListAsync
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CampusBites.Web.Pages.Admin.Roles;

[Authorize(Policy = Permissions.Users.ManageRoles)] // Secure page
public class IndexModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager; // <-- Inject UserManager
    private readonly IAuditService _auditService; // <-- Inject



    public IList<IdentityRole> Roles { get; set; } = new List<IdentityRole>();

    [BindProperty]
    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 100 characters.")]
    public string? NewRoleName { get; set; }

    [TempData]
    public string? Message { get; set; }

    // --- ADD THIS PROPERTY ---
    [TempData]
    public string? ErrorMessage { get; set; }
    // 

    public IndexModel(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IAuditService auditService) // <-- Update constructor
    {
        _roleManager = roleManager;
        _userManager = userManager; // <-- Assign UserManager
        _auditService = auditService; // <-- Assign AuditService
    }

    public async Task OnGetAsync()
    {
        // Retrieve all roles
        Roles = await _roleManager.Roles.ToListAsync();
    }

    // Handler for creating a new role
    public async Task<IActionResult> OnPostCreateRoleAsync()
    {
        // Use property value directly since it's bound
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(NewRoleName))
        {
            // If validation fails, reload the list and return the page
            Roles = await _roleManager.Roles.ToListAsync();
            return Page();
        }

        var roleExists = await _roleManager.RoleExistsAsync(NewRoleName);
        if (roleExists)
        {
            ModelState.AddModelError(nameof(NewRoleName), $"Role '{NewRoleName}' already exists.");
            Roles = await _roleManager.Roles.ToListAsync(); // Reload list before returning
            return Page();
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(NewRoleName.Trim()));
        if (result.Succeeded)
        {
            Message = $"Role '{NewRoleName}' created successfully.";
            // --- Log Audit ---
            var currentUserId = _userManager.GetUserId(User);
            await _auditService.LogAsync(
                action: "CreateRole",
                userId: currentUserId,
                details: $"Created role: {NewRoleName.Trim()}");
            // --- End Log ---
        }
        else
        {
            Message = $"Error creating role '{NewRoleName}'.";
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            Roles = await _roleManager.Roles.ToListAsync(); // Reload list before returning
            return Page();
        }

        return RedirectToPage(); // Refresh the Index page on success
    }

    // Handler for deleting a role
    public async Task<IActionResult> OnPostDeleteRoleAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound("Role ID not provided.");

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            Message = $"Role with ID '{id}' not found.";
            return RedirectToPage();
        }

        // CRITICAL CHECK: Ensure role is not assigned to any users before deleting
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!); // Use role name here
        if (usersInRole.Any())
        {
            ErrorMessage = $"Cannot delete role '{role.Name}' because it is currently assigned to {usersInRole.Count} user(s). Please remove users from this role first.";
            return RedirectToPage();
        }

        // Prevent deleting essential roles if needed (optional)
        if (role.Name == ApplicationDbInitializer.Roles.Admin || role.Name == ApplicationDbInitializer.Roles.User)
        {
            ErrorMessage = $"Cannot delete essential system role '{role.Name}'.";
            return RedirectToPage();
        }

        try
        {
            var roleName = role.Name; // Get name before deleting
            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded)
            {
                Message = $"Role '{role.Name}' deleted successfully.";
                // --- Log Audit ---
                var currentUserId = _userManager.GetUserId(User);
                await _auditService.LogAsync(
                    action: "DeleteRole",
                    userId: currentUserId,
                    entityType: "IdentityRole",
                    entityId: id, // Role ID
                    details: $"Deleted role: {roleName}");
                // --- End Log ---
            }
            else
            {
                ErrorMessage = $"Error deleting role '{role.Name}'.";
                foreach (var error in result.Errors) { ErrorMessage += $" {error.Description}"; }
            }
        }
        catch (Exception ex)
        {
            // Log error ex
            ErrorMessage = $"An unexpected error occurred deleting role '{role.Name} '. Details: {ex.Message}";
        }

        return RedirectToPage();
    }
    // --- END Delete Role Handler ---
}