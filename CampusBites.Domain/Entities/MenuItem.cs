// src/CampusBites.Domain/Entities/MenuItem.cs
namespace CampusBites.Domain.Entities;

public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Default to avoid null warnings
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; } // Nullable if image is optional
    public string Category { get; set; } = string.Empty; // E.g., "Food", "Drink", "Snack"
    public bool IsAvailable { get; set; } = true;

    // Add other relevant properties: nutritional info, ingredients, etc.
}