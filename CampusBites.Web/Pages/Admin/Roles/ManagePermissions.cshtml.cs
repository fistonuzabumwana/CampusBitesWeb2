// src/CampusBites.Web/Pages/Admin/Roles/ManagePermissions.cshtml.cs
using CampusBites.Application.Common.Security;
using CampusBites.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // Required for Claim
using System.Threading.Tasks;
using System;
using CampusBites.Application.Common.Interfaces;
using CampusBites.Infrastructure.Identity; // For Exception

namespace CampusBites.Web.Pages.Admin.Roles;

[Authorize(Policy = Permissions.Users.ManageRoles)] // Use appropriate policy
public class ManagePermissionsModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager; // <-- Inject UserManager
    private readonly IAuditService _auditService; // <-- Inject Audit Service


    // Bind the entire ViewModel
    [BindProperty]
    public ManageRolePermissionsViewModel ViewModel { get; set; } = new ManageRolePermissionsViewModel();

    [TempData]
    public string? Message { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }


    public ManagePermissionsModel(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _roleManager = roleManager; // <-- Assign RoleManager
        _userManager = userManager; // <-- Assign UserManager
        _auditService = auditService; // <-- Assign Audit Service

    }

    // Load role and permissions based on roleName from route
    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound("Role ID not provided."); // Check if ID is null or empty

        var role = await _roleManager.FindByIdAsync(id); // Find role by ID
        if (role == null) return NotFound($"Role with ID '{id}' not found."); // Check if role exists

        var roleClaims = await _roleManager.GetClaimsAsync(role); // Get claims for the role
        var rolePermissionClaims = roleClaims.Where(c => c.Type == "permission") // Filter claims for permissions
                                             .Select(c => c.Value)
                                             .ToList();

        var allPermissions = Permissions.GetAllPermissions(); // Get all defined permissions

        ViewModel = new ManageRolePermissionsViewModel
        {
            RoleId = role.Id, // Populate RoleId
            RoleName = role.Name ?? string.Empty, // Ensure RoleName is populated
            AllPermissionValues = allPermissions,
            AssignedPermissionClaims = rolePermissionClaims,
            SelectedPermissions = rolePermissionClaims // Pre-select current permissions
        };

        return Page();
    }

    // Save updated permissions for the role
    public async Task<IActionResult> OnPostAsync()
    {
        // Use ViewModel.RoleId (bound from hidden input)
        if (string.IsNullOrWhiteSpace(ViewModel.RoleId))
        {
            ErrorMessage = "Role ID was missing from the submission.";
            return RedirectToPage("./Index");
        }

        // Find role by ID now
        var role = await _roleManager.FindByIdAsync(ViewModel.RoleId);
        if (role == null) return NotFound($"Role with ID '{ViewModel.RoleId}' not found.");

        // Ensure RoleName is populated in case needed for messages etc.
        ViewModel.RoleName = role.Name ?? ViewModel.RoleName;

        var currentClaims = await _roleManager.GetClaimsAsync(role);
        var currentPermissionClaims = currentClaims.Where(c => c.Type == "permission").ToList();
        var selectedPermissions = ViewModel.SelectedPermissions ?? new List<string>();

        var errorMessages = new List<string>();
        bool changesMade = false;

        // Permissions to add: Selected but not currently assigned
        var permissionsToAdd = selectedPermissions
            .Except(currentPermissionClaims.Select(c => c.Value))
            .ToList();

        // Claims to remove: Currently assigned but no longer selected
        var claimsToRemove = currentPermissionClaims
            .Where(c => !selectedPermissions.Contains(c.Value))
            .ToList(); // List of Claim objects

        // Remove claims first
        if (claimsToRemove.Any())
        {
            foreach (var claim in claimsToRemove)
            {
                var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
                if (!removeResult.Succeeded) errorMessages.AddRange(removeResult.Errors.Select(e => $"Claim remove '{claim.Value}' failed: {e.Description}"));
                else changesMade = true;
            }
        }

        // Add new claims
        if (permissionsToAdd.Any())
        {
            foreach (var permission in permissionsToAdd)
            {
                // Prevent adding duplicate types if somehow selected multiple times
                if (!currentPermissionClaims.Any(c => c.Value == permission))
                {
                    var addResult = await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
                    if (!addResult.Succeeded) errorMessages.AddRange(addResult.Errors.Select(e => $"Claim add '{permission}' failed: {e.Description}"));
                    else changesMade = true;
                }
            }
        }

        // Set TempData messages
        if (errorMessages.Any())
        {
            ErrorMessage = "Errors occurred updating permissions: " + string.Join(" ", errorMessages);
        }
        else if (changesMade)
        {
            Message = $"Permissions for role '{ViewModel.RoleName}' updated successfully.";
            // --- Log Audit ---
            var currentUserId = _userManager.GetUserId(User);
      
            var details = $"Added: [{string.Join(", ", permissionsToAdd)}]; Removed: [{string.Join(", ", claimsToRemove.Select(c => c.Value))}]";
            await _auditService.LogAsync(
                action: "UpdateRolePermissions",
                userId: currentUserId,
                entityType: "IdentityRole",
                entityId: role.Id,
                details: $"Updated permissions for role {ViewModel.RoleName}. {details}");
            // --- End Log ---
        }
        else
        {
            Message = "No permission changes detected.";
        }

        // Redirect back to Role Index page
        return RedirectToPage("./Index");
    }
}