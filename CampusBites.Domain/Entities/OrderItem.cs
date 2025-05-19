// src/CampusBites.Domain/Entities/OrderItem.cs
namespace CampusBites.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    // Foreign Key to Order
    public int OrderId { get; set; }

    // Foreign Key to MenuItem
    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    /// <summary>
    /// Price of the MenuItem at the time the order was placed.
    /// Storing this prevents issues if MenuItem prices change later.
    /// </summary>
    public decimal Price { get; set; }

    // Navigation Properties
    // Avoid direct navigation to keep Domain simple, use IDs primarily.
    // public Order Order { get; set; } = null!;
    // public MenuItem MenuItem { get; set; } = null!;

    // --- ADD Navigation Property ---
    public MenuItem MenuItem { get; set; } = null!; // Reference to the actual MenuItem
    // --- END ADD ---
}