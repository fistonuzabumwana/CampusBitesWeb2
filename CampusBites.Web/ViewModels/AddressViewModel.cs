// src/CampusBites.Web/ViewModels/AddressViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Web.ViewModels;

public class AddressViewModel
{
    [Required(ErrorMessage = "Street address is required.")]
    [StringLength(200, ErrorMessage = "Street address cannot exceed 200 characters.")]
    [Display(Name = "Street Address")]
    public string StreetAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sector is required.")]
    [StringLength(100, ErrorMessage = "Sector cannot exceed 100 characters.")]
    public string Sector { get; set; } = string.Empty;

    [Required(ErrorMessage = "District is required.")]
    [StringLength(100, ErrorMessage = "District cannot exceed 100 characters.")]
    public string District { get; set; } = string.Empty;

    // Add Country back if needed for your context, e.g.:
    // [Required]
    // [StringLength(100)]
    // public string Country { get; set; } = "Rwanda"; // Default?
}