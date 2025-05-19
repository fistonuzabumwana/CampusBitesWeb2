// src/CampusBites.Web/ViewModels/EditRoleNameViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Web.ViewModels;

public class EditRoleNameViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty; // Hidden field

    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 100 characters.")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty; // Input field
}