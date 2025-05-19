// src/CampusBites.Web/ViewModels/EditUserViewModel.cs
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Web.ViewModels;

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Email Confirmed")]
    public bool EmailConfirmed { get; set; } // Keep if allowing edit

    [Display(Name = "Lockout Enabled")]
    public bool LockoutEnabled { get; set; } // Keep if allowing edit

    // --- Role Management Properties ---
    public List<SelectListItem> AllRoles { get; set; } = new List<SelectListItem>();
    public List<string> UserRoles { get; set; } = new List<string>(); // Current roles user has
    public List<string> SelectedRoleNames { get; set; } = new List<string>(); // Bound property for role form submission

    // --- NEW: Permission Management Properties ---
    /// <summary>
    /// List of all possible permission values defined in the application.
    /// </summary>
    public List<string> AllPermissionValues { get; set; } = new List<string>();

    /// <summary>
    /// List of permission values currently assigned directly to the user as claims.
    /// </summary>
    public List<string> AssignedPermissionClaims { get; set; } = new List<string>();

    /// <summary>
    /// List of permission values selected in the form for direct assignment (Bound Property).
    /// </summary>
    public List<string> SelectedPermissions { get; set; } = new List<string>();
}