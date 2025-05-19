// src/CampusBites.Application/DTOs/OrderSummaryDto.cs
using CampusBites.Domain.Enums; // For OrderStatus
using System;

namespace CampusBites.Application.DTOs;

public class OrderSummaryDto
{
    public int Id { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public decimal OrderTotal { get; set; }
    public OrderStatus Status { get; set; }
    public int NumberOfItems { get; set; } // Calculated field
    public string? PaymentMethod { get; set; } // Include for info

    public string UserId { get; set; } = string.Empty;
    public string? PaymentReference { get; set; } // <-- ADD THIS LINE

}