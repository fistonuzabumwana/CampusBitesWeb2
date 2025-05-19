// src/CampusBites.Web/Pages/Admin/Roles/Edit.cshtml.cs
using CampusBites.Application.Common.Security;
using CampusBites.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CampusBites.Web.Pages.Admin.Roles;

[Authorize(Policy = Permissions.Users.ManageRoles)]
public class EditModel : PageModel
{
    private readonly RoleManager<IdentityRole> _roleManager;

    [BindProperty]
    public EditRoleNameViewModel RoleViewModel { get; set; } = new EditRoleNameViewModel();

    [TempData]
    public string? Message { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }


    public EditModel(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound("Role ID not provided.");

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound($"Role with ID '{id}' not found.");

        RoleViewModel = new EditRoleNameViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var role = await _roleManager.FindByIdAsync(RoleViewModel.Id);
        if (role == null) return NotFound($"Role with ID '{RoleViewModel.Id}' not found.");

        // Check if new name already exists (case-insensitive)
        var normalizedNewName = _roleManager.NormalizeKey(RoleViewModel.Name);
        var existingRole = await _roleManager.FindByNameAsync(normalizedNewName);
        if (existingRole != null && existingRole.Id != role.Id)
        {
            ModelState.AddModelError(nameof(RoleViewModel.Name), $"Role name '{RoleViewModel.Name}' is already taken.");
            return Page();
        }

        // Update role name
        role.Name = RoleViewModel.Name;
        role.NormalizedName = normalizedNewName; // Important to update normalized name too!

        var result = await _roleManager.UpdateAsync(role);

        if (result.Succeeded)
        {
            Message = $"Role '{role.Name}' updated successfully.";
            return RedirectToPage("./Index");
        }
        else
        {
            ErrorMessage = "Error updating role.";
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }
    }
}