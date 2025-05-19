// src/CampusBites.Infrastructure/Persistence/Repositories/OrderRepository.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Domain.Entities;
using CampusBites.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CampusBites.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IApplicationDbContext _context;

    public OrderRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Order> AddAsync(Order order)
    {
        // Add the Order entity. EF Core's change tracker will automatically
        // detect the related OrderItems in the order.OrderItems collection
        // (if populated) and mark them as Added as well, assuming the relationship
        // was configured correctly in OnModelCreating.
        _context.Orders.Add(order);

        // Return the order object. Its Id and the Ids of its OrderItems
        // will be populated after SaveChangesAsync is called elsewhere.
        return Task.FromResult(order);
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders.FindAsync(id);
    }

    // Implement GetByIdAsync, GetByUserIdAsync etc. later if needed
    // These would typically involve using .Include() for OrderItems/Addresses
    // public async Task<Order?> GetByIdAsync(int id, bool includeItems = false)
    // {
    //     IQueryable<Order> query = _context.Orders;
    //     if (includeItems)
    //     {
    //          query = query.Include(o => o.OrderItems);
    //          // .ThenInclude(oi => oi.MenuItem); // If needed, requires MenuItem nav prop
    //          // .Include(o => o.ShippingAddress) // If needed, requires Address nav prop
    //          // .Include(o => o.BillingAddress); // If needed, requires Address nav prop
    //     }
    //     return await query.FirstOrDefaultAsync(o => o.Id == id);
    // }
    //
    // public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
    // {
    //      return await _context.Orders
    //          .Where(o => o.UserId == userId)
    //          .OrderByDescending(o => o.OrderDate) // Example ordering
    //          // Consider including OrderItems for summary display?
    //          // .Include(o => o.OrderItems)
    //          .ToListAsync();
    // }
    // Add this method implementation
    public Task UpdateAsync(Order order)
    {
        // EF Core tracks changes to the entity. Just marking it as Modified
        // ensures it will be updated when SaveChangesAsync is called.
        // If the entity was fetched within the same context scope, modifying its
        // properties might be enough, but explicit marking is safer.
        _context.Entry(order).State = EntityState.Modified;
        return Task.CompletedTask; // No async DB work here, just state change
    }

    // --- IMPLEMENT Order Retrieval Methods ---

    public async Task<Order?> GetByIdAsync(int id, bool includeRelated = false)
    {
        // Start with the base query for Orders
        IQueryable<Order> query = _context.Orders;

        // Conditionally include related data if requested
        if (includeRelated)
        {
            query = query
                .Include(o => o.OrderItems)         // Include the list of OrderItems
                    .ThenInclude(oi => oi.MenuItem) // For each OrderItem, include its MenuItem
                .Include(o => o.ShippingAddress)    // Include the Shipping Address
                .Include(o => o.BillingAddress);     // Include the Billing Address
        }

        // Filter by ID and return the first result or null
        return await query.FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<Order>> GetByUserIdAsync(string userId)
    {
        // Start query, filter by UserId
        var query = _context.Orders
            .Where(o => o.UserId == userId);

        // For the history list, include OrderItems to calculate item count easily.
        // We might not need addresses or full menu item details here yet.
        query = query.Include(o => o.OrderItems);

        // Order by date, newest first
        query = query.OrderByDescending(o => o.OrderDate);

        // Execute the query and return the list
        return await query.ToListAsync();
    }
    // --- END IMPLEMENT ---

    // --- IMPLEMENT New Methods ---
    public async Task<IEnumerable<Order>> GetAllAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var query = _context.Orders.Include(o => o.OrderItems).AsQueryable();

        if (startDate.HasValue)
        {
            var utcStart = startDate.Value.ToUniversalTime(); // Convert to UTC
            query = query.Where(o => o.OrderDate >= utcStart);
        }
        if (endDate.HasValue)
        {
            // Add 1 day to include the whole end date, then convert to UTC
            var utcEnd = endDate.Value.Date.AddDays(1).ToUniversalTime();
            query = query.Where(o => o.OrderDate < utcEnd);
        }

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    public async Task<int> GetOrderCountAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var query = _context.Orders.AsQueryable();

        if (startDate.HasValue)
        {
            var utcStart = startDate.Value.ToUniversalTime(); // Convert to UTC
            query = query.Where(o => o.OrderDate >= utcStart);
        }
        if (endDate.HasValue)
        {
            var utcEnd = endDate.Value.Date.AddDays(1).ToUniversalTime(); // Convert to UTC
            query = query.Where(o => o.OrderDate < utcEnd);
        }

        return await query.CountAsync();
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        var query = _context.Orders.AsQueryable();
        // Filter by status first
        query = query.Where(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered || o.Status == OrderStatus.ReadyForPickup);

        if (startDate.HasValue)
        {
            var utcStart = startDate.Value.ToUniversalTime(); // Convert to UTC
            query = query.Where(o => o.OrderDate >= utcStart);
        }
        if (endDate.HasValue)
        {
            var utcEnd = endDate.Value.Date.AddDays(1).ToUniversalTime(); // Convert to UTC
            query = query.Where(o => o.OrderDate < utcEnd);
        }

        // SumAsync can return null if no orders match, default to 0
        return await query.SumAsync(o => (decimal?)o.OrderTotal) ?? 0m; // Use nullable decimal for SumAsync
    }
    // --- END IMPLEMENT ---
}