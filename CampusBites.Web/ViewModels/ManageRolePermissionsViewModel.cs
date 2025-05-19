// src/CampusBites.Web/ViewModels/ManageRolePermissionsViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Web.ViewModels;

public class ManageRolePermissionsViewModel
{
    [Required]
    public string RoleId { get; set; } = string.Empty; // <-- Added RoleId


    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// All possible permission values defined in Permissions class.
    /// </summary>
    public List<string> AllPermissionValues { get; set; } = new List<string>();

    /// <summary>
    /// Permission values currently assigned to this role.
    /// </summary>
    public List<string> AssignedPermissionClaims { get; set; } = new List<string>();

    /// <summary>
    /// List of permission values selected in the form (Bound Property).
    /// </summary>
    public List<string> SelectedPermissions { get; set; } = new List<string>();
}