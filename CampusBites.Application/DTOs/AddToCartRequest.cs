// src/CampusBites.Application/DTOs/AddToCartRequest.cs
namespace CampusBites.Application.DTOs;

public class AddToCartRequest
{
    public int Quantity { get; set; } = 1; // Default quantity to 1
}