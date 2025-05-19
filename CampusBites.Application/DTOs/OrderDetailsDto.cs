// src/CampusBites.Application/DTOs/OrderDetailsDto.cs
using CampusBites.Domain.Enums;
using CampusBites.Application.DTOs; // Use this namespace now
// using CampusBites.Web.ViewModels; // REMOVE THIS LINE
using System;
using System.Collections.Generic;

namespace CampusBites.Application.DTOs;

public class OrderDetailsDto
{
    public int Id { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public decimal OrderTotal { get; set; }
    public OrderStatus Status { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentReference { get; set; }

    // Change property types to AddressDto
    public AddressDto ShippingAddress { get; set; } = new AddressDto();
    public AddressDto BillingAddress { get; set; } = new AddressDto();

    public List<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
}