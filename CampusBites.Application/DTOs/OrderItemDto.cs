// src/CampusBites.Application/DTOs/OrderItemDto.cs
namespace CampusBites.Application.DTOs;

public class OrderItemDto
{
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty; // Get name from related MenuItem
    public int Quantity { get; set; }
    public decimal Price { get; set; } // Price paid at time of order
    public decimal Subtotal => Price * Quantity;
}