// src/CampusBites.Web/Pages/Admin/Users/Edit.cshtml.cs
using CampusBites.Application.Common.Security;
using CampusBites.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims; // <-- Add for Claim
using System.Threading.Tasks;
using System;
using CampusBites.Application.Services;
using CampusBites.Application.Common.Interfaces;
using CampusBites.Infrastructure.Identity;

namespace CampusBites.Web.Pages.Admin.Users;

[Authorize(Policy = Permissions.Users.ManageRoles)] // Policy might need adjustment if editing user props too
public class EditModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    [BindProperty]
    public EditUserViewModel UserViewModel { get; set; } = new EditUserViewModel();

    [TempData]
    public string? Message { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    private readonly IAuditService _auditService; // Inject Audit Service


    public EditModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound("User ID not provided.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound($"User with ID '{id}' not found.");

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.ToListAsync();
        // --- Load User Claims ---
        var userClaims = await _userManager.GetClaimsAsync(user);
        var userPermissionClaims = userClaims.Where(c => c.Type == "permission")
                                             .Select(c => c.Value)
                                             .ToList();
        // --- Load All Defined Permissions ---
        var allDefinedPermissions = Permissions.GetAllPermissions();


        UserViewModel = new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? "N/A",
            Email = user.Email ?? "N/A",
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            // Role Info
            UserRoles = userRoles.ToList(),
            AllRoles = allRoles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList(),
            SelectedRoleNames = userRoles.ToList(), // Pre-select current roles
            // Permission Info
            AllPermissionValues = allDefinedPermissions,
            AssignedPermissionClaims = userPermissionClaims,
            SelectedPermissions = userPermissionClaims // Pre-select current direct permissions
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.FindByIdAsync(UserViewModel.Id);
        if (user == null) return NotFound($"User with ID '{UserViewModel.Id}' not found.");

        bool changesMade = false;
        var errorMessages = new List<string>();

        // --- Update Roles ---
        var currentRoles = await _userManager.GetRolesAsync(user);
        var selectedRoles = UserViewModel.SelectedRoleNames ?? new List<string>();
        var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

        if (rolesToAdd.Any())
        {
            var addRoleResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addRoleResult.Succeeded) errorMessages.AddRange(addRoleResult.Errors.Select(e => $"Role add failed: {e.Description}"));
            else changesMade = true;
        }
        if (rolesToRemove.Any())
        {
            var removeRoleResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeRoleResult.Succeeded) errorMessages.AddRange(removeRoleResult.Errors.Select(e => $"Role remove failed: {e.Description}"));
            else changesMade = true;
        }

        // --- Update Direct User Permission Claims ---
        var currentClaims = await _userManager.GetClaimsAsync(user);
        var currentPermissionClaims = currentClaims.Where(c => c.Type == "permission").ToList();
        var selectedPermissions = UserViewModel.SelectedPermissions ?? new List<string>();

        var permissionsToAdd = selectedPermissions
            .Except(currentPermissionClaims.Select(c => c.Value))
            .Select(p => new Claim("permission", p)) // Create Claim objects
            .ToList();

        var claimsToRemove = currentPermissionClaims
            .Where(c => !selectedPermissions.Contains(c.Value))
            .ToList(); // List of Claim objects

        if (claimsToRemove.Any())
        {
            var removeClaimResult = await _userManager.RemoveClaimsAsync(user, claimsToRemove);
            if (!removeClaimResult.Succeeded) errorMessages.AddRange(removeClaimResult.Errors.Select(e => $"Claim remove failed: {e.Description}"));
            else changesMade = true;
        }
        if (permissionsToAdd.Any())
        {
            var addClaimResult = await _userManager.AddClaimsAsync(user, permissionsToAdd);
            if (!addClaimResult.Succeeded) errorMessages.AddRange(addClaimResult.Errors.Select(e => $"Claim add failed: {e.Description}"));
            else changesMade = true;
        }

        // --- Set TempData messages ---
        if (errorMessages.Any())
        {
            ErrorMessage = "Errors occurred: " + string.Join(" ", errorMessages);
        }
        else if (changesMade)
        {
            Message = $"User '{user.UserName}' roles/permissions updated successfully.";
            // --- Log Audit ---
            var currentUserId = _userManager.GetUserId(User); // Admin performing action
                                                              // Create details string (simple version)
            var details = $"Roles Added: [{string.Join(",", rolesToAdd)}], Removed: [{string.Join(",", rolesToRemove)}]. Claims Added: [{string.Join(",", permissionsToAdd.Select(p => p.Value))}], Removed: [{string.Join(",", claimsToRemove.Select(c => c.Value))}]";
            await _auditService.LogAsync(
                action: "UpdateUserRolesPermissions",
                userId: currentUserId,
                entityType: "ApplicationUser",
                entityId: user.Id, // ID of user being edited
                details: $"Updated roles/permissions for user {user.UserName}. {details}");
            // --- End Log ---
        }
        else
        {
            Message = "No changes detected.";
        }

        // If returning Page due to errors, need to re-populate lists
        if (errorMessages.Any())
        {
            var allRoles = await _roleManager.Roles.ToListAsync();
            UserViewModel.AllRoles = allRoles.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
            UserViewModel.UserRoles = (await _userManager.GetRolesAsync(user)).ToList(); // Re-get current roles
            UserViewModel.AllPermissionValues = Permissions.GetAllPermissions();
            UserViewModel.AssignedPermissionClaims = (await _userManager.GetClaimsAsync(user)).Where(c => c.Type == "permission").Select(c => c.Value).ToList(); // Re-get current claims
                                                                                                                                                                 // Keep submitted selections for roles/permissions in UserViewModel
            return Page();
        }

        return RedirectToPage("./Index");
    }
}