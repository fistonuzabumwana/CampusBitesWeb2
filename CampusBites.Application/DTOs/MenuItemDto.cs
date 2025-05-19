// src/CampusBites.Application/DTOs/MenuItemDto.cs
namespace CampusBites.Application.DTOs;

// Represents the data shape for a MenuItem exposed by the Application layer
public class MenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }

    // Add other properties if needed by the UI/API,
    // but avoid including properties irrelevant to the consumer.
}