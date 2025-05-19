// src/CampusBites.Domain/Entities/AuditLog.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; } // Use long for potentially high volume

    [Required]
    public DateTimeOffset Timestamp { get; set; } // Time of the event (UTC)

    [MaxLength(450)] // Matches IdentityUser Id max length
    public string? UserId { get; set; } // Nullable for system actions or anonymous

    [Required]
    [MaxLength(256)]
    public string Action { get; set; } = string.Empty; // e.g., "MenuItemCreated", "UserRoleUpdated"

    [MaxLength(100)]
    public string? EntityType { get; set; } // e.g., "MenuItem", "IdentityUser"

    [MaxLength(450)] // Match key lengths
    public string? EntityId { get; set; } // Primary key of the affected entity

    [MaxLength(2000)] // Store details like changes or context
    public string? Details { get; set; }
}