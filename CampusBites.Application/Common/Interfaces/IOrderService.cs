// src/CampusBites.Application/Common/Interfaces/IOrderService.cs
using CampusBites.Application.DTOs;
using CampusBites.Domain.Entities; // Required for Address, CartItem
using CampusBites.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;
// using CampusBites.Application.DTOs; // Add later for return types like OrderDto

namespace CampusBites.Application.Common.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Creates a new order based on the user's cart and address details.
    /// </summary>
    /// <param name="userId">The ID of the user placing the order.</param>
    /// <param name="cartItems">The list of items from the user's cart.</param>
    /// <param name="shippingAddress">The shipping address entity.</param>
    /// <param name="billingAddress">The billing address entity.</param>
    /// <returns>The ID of the newly created order.</returns>
    /// <exception cref="System.ArgumentException">Thrown if cart is empty or other validation fails.</exception>
    /// <exception cref="System.Exception">Thrown if saving fails.</exception>
    // Update signature
    Task<int> CreateOrderAsync(string userId, List<CartItem> cartItems, Address shippingAddress, Address billingAddress, string? paymentMethod, string? paymentReference);
    // Add other methods later for retrieving order details or history
    // Task<OrderDto?> GetOrderDetailsAsync(int orderId, string userId);
    // Task<IEnumerable<OrderSummaryDto>> GetUserOrdersAsync(string userId);
    // Add this method signature
    Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus); // Returns true if successful
                                                                           // --- ADD Order Retrieval Methods ---
    /// <summary>
    /// Gets a summary of all orders for a specific user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>A collection of OrderSummaryDto.</returns>
    Task<IEnumerable<OrderSummaryDto>> GetUserOrdersAsync(string userId);

    /// <summary>
    /// Gets the full details for a specific order belonging to a specific user.
    /// </summary>
    /// <param name="orderId">The ID of the order to retrieve.</param>
    /// <param name="userId">The ID of the user requesting the order (for verification).</param>
    /// <returns>An OrderDetailsDto if the order exists and belongs to the user; otherwise, null.</returns>
    Task<OrderDetailsDto?> GetOrderDetailsAsync(int orderId, string userId);
    // --- END ADD ---

    // --- ADD/UPDATE These ---
    /// <summary>
    /// Gets summaries for all orders, optionally filtered by date.
    /// </summary>
    Task<IEnumerable<OrderSummaryDto>> GetAllOrderSummariesAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Gets sales summary statistics for a given period name and date range.
    /// </summary>
    Task<SalesSummaryDto> GetSalesSummaryAsync(string periodName, DateTimeOffset startDate, DateTimeOffset endDate);
    // --- END ADD/UPDATE ---
}