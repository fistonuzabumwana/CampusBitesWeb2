// src/CampusBites.Domain/Entities/Address.cs
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Domain.Entities;

public class Address
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string StreetAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Sector { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string District { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;
}