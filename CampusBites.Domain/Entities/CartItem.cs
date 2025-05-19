// src/CampusBites.Domain/Entities/CartItem.cs
namespace CampusBites.Domain.Entities;

public class CartItem
{
    // Note: No Id needed if not storing in DB permanently yet
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty; // Store name for easy display
    public decimal Price { get; set; } // Store price for easy display/calculation
    public string? ImageUrl { get; set; } // Optional image
    public string? Category { get; set; }
    public int Quantity { get; set; }

    // If you want the full MenuItem object:
    // public MenuItem MenuItem { get; set; } = null!;
    // public int Quantity { get; set; }
    // However, storing only necessary details might be simpler for session state.
}