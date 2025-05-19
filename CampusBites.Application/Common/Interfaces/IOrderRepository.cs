// src/CampusBites.Application/Common/Interfaces/IOrderRepository.cs
using CampusBites.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic; // If adding Get methods later

namespace CampusBites.Application.Common.Interfaces;

public interface IOrderRepository
{
    /// <summary>
    /// Adds a new order (including its OrderItems) to the data store.
    /// </summary>
    /// <param name="order">The order entity to add, with OrderItems populated.</param>
    /// <returns>The added order entity, possibly with updated Id and OrderItem Ids.</returns>
    Task<Order> AddAsync(Order order);


    // Add other methods later for order history, admin views etc.:
    // Task<Order?> GetByIdAsync(int id, bool includeItems = false);
    // Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    // Task UpdateOrderAsync(Order order); // For status updates etc.
    // Add this method signature
    Task UpdateAsync(Order order);
    // --- ADD Order Retrieval Methods ---
    /// <summary>
    /// Gets an order by its ID, optionally including related data.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="includeRelated">Whether to include OrderItems and Addresses.</param>
    /// <returns>The order if found; otherwise, null.</returns>
    Task<Order?> GetByIdAsync(int id, bool includeRelated = false);

    /// <summary>
    /// Gets all orders placed by a specific user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>A collection of the user's orders.</returns>
    Task<IEnumerable<Order>> GetByUserIdAsync(string userId);
    // --- END ADD ---

    Task<Order?> GetByIdAsync(int id);

    // --- ADD/UPDATE These ---
    /// <summary>
    /// Gets all orders, optionally filtered by date range. Includes OrderItems.
    /// </summary>
    Task<IEnumerable<Order>> GetAllAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Gets the count of orders within an optional date range.
    /// </summary>
    Task<int> GetOrderCountAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);

    /// <summary>
    /// Gets the sum of OrderTotal for orders within an optional date range.
    /// </summary>
    Task<decimal> GetTotalRevenueAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null);
    // --- END ADD/UPDATE ---
}