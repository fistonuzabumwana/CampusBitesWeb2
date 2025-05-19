// src/CampusBites.Infrastructure/Identity/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Infrastructure.Identity;

// Inherit from IdentityUser
public class ApplicationUser : IdentityUser
{
    // Add your custom properties here
    [PersonalData] // Mark as personal data for GDPR/download purposes
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [PersonalData]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [PersonalData]
    [MaxLength(500)] // Store URL/path to the picture
    public string? ProfilePictureUrl { get; set; }

    // Phone number is already part of the base IdentityUser
}