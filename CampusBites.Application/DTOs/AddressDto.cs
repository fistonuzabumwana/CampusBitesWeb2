// src/CampusBites.Application/DTOs/AddressDto.cs
namespace CampusBites.Application.DTOs;

// DTO for representing Address data within the Application layer
public class AddressDto
{
    // Usually don't expose Id in DTOs unless necessary for client linking
    // public int Id { get; set; }

    public string StreetAddress { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    // Add Country back if it's part of your Address entity and needed here

    // Usually don't expose UserId in address DTOs sent to client
    // public string UserId { get; set; } = string.Empty;
}