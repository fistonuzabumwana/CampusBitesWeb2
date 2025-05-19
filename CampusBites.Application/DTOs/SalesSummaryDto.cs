// src/CampusBites.Application/DTOs/SalesSummaryDto.cs
using System;

namespace CampusBites.Application.DTOs;

public class SalesSummaryDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTimeOffset StartDate { get; set; } // For context
    public DateTimeOffset EndDate { get; set; }   // For context
    public string PeriodName { get; set; } = string.Empty; // e.g., "Today", "This Week"
}