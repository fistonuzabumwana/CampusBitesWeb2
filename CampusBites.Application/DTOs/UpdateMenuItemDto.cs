// src/CampusBites.Application/DTOs/UpdateMenuItemDto.cs
using Microsoft.AspNetCore.Http; // Needed for IFormFile
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Application.DTOs;

public class UpdateMenuItemDto
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Item name is required.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, 1000000.00, ErrorMessage = "Price must be between RWF 0.01 and RWF 1,000,000.00.")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters.")]
    public string Category { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    // Replaced ImageUrl string with IFormFile for upload
    public IFormFile? ImageFile { get; set; } // Nullable allows updating without changing the image
}