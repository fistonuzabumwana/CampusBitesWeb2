// src/CampusBites.Domain/Entities/Order.cs
using CampusBites.Domain.Enums; // For OrderStatus
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusBites.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    // Foreign Key to IdentityUser (string representation)
    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

    public decimal OrderTotal { get; set; } // Calculated total

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // Foreign Keys to Address entity
    public int ShippingAddressId { get; set; }
    public int BillingAddressId { get; set; }

    // --- NEW: Payment Information ---
    [MaxLength(50)] // e.g., "MTNMoMo", "AirtelMoney", "SimulatedCard"
    public string? PaymentMethod { get; set; }

    [MaxLength(100)] // To store phone number or other reference
    public string? PaymentReference { get; set; }
    // --- END NEW ---

    // Navigation Properties
    // We define the relationship via FK IDs above.
    // Actual Address objects can be loaded via services if needed.
    // public Address ShippingAddress { get; set; } = null!;
    // public Address BillingAddress { get; set; } = null!;

    // Collection of items in this order
    public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // --- ADD Navigation Properties ---
    public Address ShippingAddress { get; set; } = null!;
    public Address BillingAddress { get; set; } = null!;
    // --- END ADD ---

}