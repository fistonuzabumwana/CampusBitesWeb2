// src/CampusBites.Domain/Enums/OrderStatus.cs
namespace CampusBites.Domain.Enums;

public enum OrderStatus
{
    Pending,      // Order placed, awaiting payment/processing
    Processing,   // Order is being prepared
    Shipped,      // Order has been shipped (if applicable)
    ReadyForPickup, // Order is ready for pickup (if applicable)
    Delivered,    // Order received by customer
    Completed,    // Order fully completed (alternative final state)
    Cancelled     // Order was cancelled
}