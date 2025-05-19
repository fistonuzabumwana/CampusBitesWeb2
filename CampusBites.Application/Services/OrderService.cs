using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.DTOs;
using CampusBites.Domain.Entities;
using CampusBites.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Optional: For logging
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusBites.Infrastructure.Identity;


namespace CampusBites.Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IApplicationDbContext _context; // For SaveChangesAsync (Unit of Work)
    private readonly ILogger<OrderService> _logger;   // Optional: For logging
    private readonly IDashboardNotifier _dashboardNotifier; // <-- Inject Notifier
    private readonly IEmailSender _emailSender; // <-- Inject Email Sender
    private readonly UserManager<ApplicationUser> _userManager; // <-- Inject User Manager
    private readonly IAuditService _auditService; // <-- Inject Audit Service




    public OrderService(
        IOrderRepository orderRepository,
        IAddressRepository addressRepository,
        IApplicationDbContext context,
        ILogger<OrderService> logger,
        IDashboardNotifier dashboardNotifier,
        IEmailSender emailSender, // <-- Add param
        UserManager<ApplicationUser> userManager,
        IAuditService auditService) // <-- Add param) // <-- Add Notifier to constructor
    {
        _orderRepository = orderRepository;
        _addressRepository = addressRepository;
        _context = context;
        _logger = logger;
        _dashboardNotifier = dashboardNotifier; // <-- Assign Notifier
        _emailSender = emailSender; // <-- Assign
        _userManager = userManager; // <-- Assign
        _auditService = auditService;
    }

    public async Task<int> CreateOrderAsync(string userId, List<CartItem> cartItems, Address shippingAddress, Address billingAddress, string? paymentMethod, string? paymentReference)
    {
        // 1. Validation (same as before)
        if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
        if (cartItems == null || !cartItems.Any()) throw new ArgumentException("Cart cannot be empty.", nameof(cartItems));
        if (shippingAddress == null) throw new ArgumentNullException(nameof(shippingAddress));
        if (billingAddress == null) throw new ArgumentNullException(nameof(billingAddress));

        _logger.LogInformation("Creating order for User ID: {UserId}", userId);

        // --- Use Explicit Transaction ---
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 2. Prepare and Save Addresses to get IDs
            shippingAddress.UserId = userId;
            billingAddress.UserId = userId;

            // Add addresses via repository (marks them as Added)
            await _addressRepository.AddAsync(shippingAddress);
            await _addressRepository.AddAsync(billingAddress);

            // Save changes specifically to generate Address IDs
            await _context.SaveChangesAsync(CancellationToken.None);

            // IDs are now populated on shippingAddress and billingAddress objects
            _logger.LogInformation("Saved Shipping Address ID: {ShippingId}, Billing Address ID: {BillingId}", shippingAddress.Id, billingAddress.Id);


            // 3. Prepare Order using generated Address IDs
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTimeOffset.UtcNow,
                Status = OrderStatus.Pending,
                ShippingAddressId = shippingAddress.Id,
                BillingAddressId = billingAddress.Id,
                // --- Populate Payment Info ---
                PaymentMethod = paymentMethod,
                PaymentReference = paymentReference,
                // --- End Populate ---
                OrderItems = new List<OrderItem>()
            };

            decimal orderTotal = 0;
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    // OrderId will be set by EF Core during the *next* SaveChanges
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                order.OrderItems.Add(orderItem);
                orderTotal += (item.Price * item.Quantity);
            }
            order.OrderTotal = orderTotal;

            // 4. Add Order (with OrderItems) via repository
            await _orderRepository.AddAsync(order);

            // 5. Save Order and OrderItems
            await _context.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation("Saved Order ID: {OrderId} with {ItemCount} items.", order.Id, order.OrderItems.Count);
            // --- Log Audit ---
            await _auditService.LogAsync(
                action: "PlaceOrder",
                userId: userId, // User placing the order
                entityType: nameof(Order),
                entityId: order.Id.ToString(),
                details: $"Order placed with total {order.OrderTotal:C} using {paymentMethod}.");
            // --- End Log ---

            // 6. Commit Transaction
            await transaction.CommitAsync();
            _logger.LogInformation("Saved Order ID: {OrderId}", order.Id);

            // --- SEND ORDER CONFIRMATION EMAIL ---
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user?.Email != null) // Check if user and email exist
                {
                    string subject = $"CampusBites Order Confirmation #{order.Id}";
                    string message = $@"
                         <h1>Thank you for your order!</h1>
                         <p>Your order with ID <strong>{order.Id}</strong> has been placed successfully on {order.OrderDate:f}.</p>
                         <p>Total Amount: {order.OrderTotal:C}</p>
                         <p>Status: {order.Status}</p>
                         <p>We'll notify you when its status updates.</p>
                         <hr/>
                         <p>Items:</p>
                         <ul>";
                    foreach (var item in order.OrderItems)
                    {
                        // Ideally fetch item name here if not stored in CartItem/OrderItem
                        message += $"<li>{item.Quantity} x ItemId {item.MenuItemId} @ {item.Price:C} each</li>";
                    }
                    message += "</ul><p>Thank you for choosing CampusBites!</p>";

                    await _emailSender.SendEmailAsync(user.Email, subject, message);
                    _logger.LogInformation("Order confirmation email sent successfully for Order ID {OrderId} to {Email}", order.Id, user.Email);
                }
                else
                {
                    _logger.LogWarning("Could not send order confirmation email for Order ID {OrderId}: User or User Email not found.", order.Id);
                }
            }
            catch (Exception emailEx)
            {
                // Log email error but don't fail the whole order process
                _logger.LogError(emailEx, "Failed to send order confirmation email for Order ID {OrderId}", order.Id);
            }
            // --- END SEND EMAIL ---

            // --- SEND NOTIFICATION ---
            try
            {
                // Map the created order to summary DTO for notification
                var orderSummary = MapToOrderSummaryDto(order); // Use existing helper
                await _dashboardNotifier.NotifyNewOrderAsync(orderSummary);
            }
            catch (Exception notificationEx)
            {
                // Log notification error but don't fail the whole operation
                _logger.LogError(notificationEx, "Failed to send new order notification for Order ID {OrderId}", order.Id);
            }
            // --- END SEND NOTIFICATION ---
            // 7. Return the new Order ID
            return order.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for User ID: {UserId}. Rolling back transaction.", userId);
            // Attempt to roll back the transaction on any error
            await transaction.RollbackAsync();
            // Re-throw the original exception
            throw;
        }
    }

    // Implement GetOrderDetailsAsync, GetUserOrdersAsync later...

    // Add this method implementation
    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        // Note: Need GetById implementation in repository or fetch directly here
        // Let's assume fetching directly here for simplicity, requires DbSet on interface
        // OR add GetById to IOrderRepository first. Let's add GetById to repo.

        // *** First, add GetByIdAsync to IOrderRepository and OrderRepository ***

        // IOrderRepository.cs:
        // Task<Order?> GetByIdAsync(int id);

        // OrderRepository.cs:
        // public async Task<Order?> GetByIdAsync(int id)
        // {
        //     return await _context.Orders.FindAsync(id);
        // }
        // ***********************************************************************


        // Now implement in OrderService:
        var order = await _orderRepository.GetByIdAsync(orderId); // Use the repository method
        if (order == null)
        {
            _logger.LogWarning("Attempted to update status for non-existent Order ID: {OrderId}", orderId);
            return false; // Order not found
        }
        var oldStatus = order.Status; // Capture old status for logging
        order.Status = newStatus;
        await _orderRepository.UpdateAsync(order); // Mark as modified

        try
        {
            await _context.SaveChangesAsync(CancellationToken.None); // Save the change
            _logger.LogInformation("Updated status for Order ID: {OrderId} to {Status}", orderId, newStatus);
            // For now, logging without the admin ID:
            await _auditService.LogAsync(
                action: "UpdateOrderStatus",
                // userId: adminUserId, // ID of admin who made the change
                entityType: nameof(Order),
                entityId: order.Id.ToString(),
                details: $"Order status changed from {oldStatus} to {newStatus}.");
            // --- End Log ---
            // --- Send Status Update Email (Conditional) ---
            var user = await _userManager.FindByIdAsync(order.UserId);
            if (user?.Email != null)
            {
                // Check user preference claim
                var claims = await _userManager.GetClaimsAsync(user);
                var notifyPref = claims.FirstOrDefault(c => c.Type == "notify_pref_order_status_email")?.Value;

                // Send only if preference is "true" (or if claim doesn't exist - defaulting to notify)
                if (notifyPref?.ToLower() != "false") // Only skip if explicitly set to false
                {
                    try
                    {
                        string subject = $"CampusBites Order #{order.Id} Status Update: {newStatus}";
                        string message = $@"
<html>
  <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
    <table width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width: 600px; margin: auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
      <tr>
        <td>
          <h2 style='color: #2c3e50;'>Your Order Status Has Changed</h2>
          <p style='font-size: 16px; color: #333333;'>
            The status of your <strong>CampusBites</strong> order with ID <strong>{order.Id}</strong> 
            (placed on {order.OrderDate:f}) has been updated to: 
            <span style='font-weight: bold; color: #27ae60;'>{newStatus}</span>.
          </p>
          <p style='font-size: 16px; color: #333333;'>
            <strong>Order Total:</strong> <span style='color: #2980b9;'>{order.OrderTotal:C}</span>
          </p>
          <p style='font-size: 16px;'>
    
          </p>
          <p style='font-size: 16px; color: #333333;'>Thank you for using CampusBites!</p>
        </td>
      </tr>
    </table>
  </body>
</html>";


                        await _emailSender.SendEmailAsync(user.Email, subject, message);
                        _logger.LogInformation("Order status update email sent successfully for Order ID {OrderId} to {Email}", order.Id, user.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send order status update email for Order ID {OrderId}", order.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Skipping order status update email for Order ID {OrderId} to {Email} due to user preference.", order.Id, user.Email);
                }
            }
            else { _logger.LogWarning("Could not send order status update email for Order ID {OrderId}: User or User Email not found.", order.Id); }
            // --- End Send Email ---
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update failed while updating status for Order ID: {OrderId}", orderId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating status for Order ID: {OrderId}", orderId);
            return false;
        }
    }

    // --- NEW Order Retrieval Methods ---

    public async Task<IEnumerable<OrderSummaryDto>> GetUserOrdersAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Enumerable.Empty<OrderSummaryDto>(); // Return empty list if no user ID
        }

        _logger.LogInformation("Fetching orders for User ID: {UserId}", userId);
        var orders = await _orderRepository.GetByUserIdAsync(userId); // Repo includes OrderItems

        // Map the Order entities to OrderSummaryDto objects
        return orders.Select(order => MapToOrderSummaryDto(order));
    }

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(int orderId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Attempted to get order details with empty UserId for Order ID: {OrderId}", orderId);
            return null; // Cannot verify ownership
        }

        _logger.LogInformation("Fetching details for Order ID: {OrderId} for User ID: {UserId}", orderId, userId);
        // Fetch order including related items and addresses
        var order = await _orderRepository.GetByIdAsync(orderId, includeRelated: true);

        // Check if order exists and belongs to the specified user
        if (order == null || order.UserId != userId)
        {
            _logger.LogWarning("Order details access denied or order not found. Order ID: {OrderId}, Requested by User ID: {UserId}", orderId, userId);
            return null; // Not found or access denied
        }

        if (order.UserId != userId)
        {
            _logger.LogWarning("Order details access denied. Order ID: {OrderId}, Requested by User ID: {UserId}", orderId, userId);
            // Don't return data that doesn't belong to the user
            return null; // Access denied
        }
        // Map the full Order entity (with related data) to OrderDetailsDto
        return MapToOrderDetailsDto(order);
    }

    // --- Private Mapping Helper Methods ---

    private static OrderSummaryDto MapToOrderSummaryDto(Order order)
    {
        return new OrderSummaryDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            OrderTotal = order.OrderTotal,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            // Calculate number of items (requires OrderItems to be loaded by repo)
            NumberOfItems = order.OrderItems?.Sum(oi => oi.Quantity) ?? 0,
            UserId = order.UserId,
            PaymentReference = order.PaymentReference // <-- ADD THIS LINE

        };
    }

    private static OrderDetailsDto MapToOrderDetailsDto(Order order)
    {
        // Assumes OrderItems (with MenuItem) and Addresses are loaded via Include() in repo
        return new OrderDetailsDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            OrderTotal = order.OrderTotal,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            PaymentReference = order.PaymentReference,
            ShippingAddress = MapAddressToDto(order.ShippingAddress),
            BillingAddress = MapAddressToDto(order.BillingAddress),
            OrderItems = order.OrderItems?.Select(oi => MapOrderItemToDto(oi)).ToList() ?? new List<OrderItemDto>()
        };
    }

    private static AddressDto MapAddressToDto(Address address)
    {
        // Handle potential null address if relationship wasn't required/loaded
        if (address == null) return new AddressDto();

        return new AddressDto
        {
            StreetAddress = address.StreetAddress,
            Sector = address.Sector,
            District = address.District
            // Map Country if added
        };
    }

    private static OrderItemDto MapOrderItemToDto(OrderItem orderItem)
    {
        // Handle potential null item/menuitem if relationship wasn't loaded properly
        if (orderItem == null) return new OrderItemDto();

        return new OrderItemDto
        {
            MenuItemId = orderItem.MenuItemId,
            // Use MenuItem navigation property (requires ThenInclude in repo GetByIdAsync)
            MenuItemName = orderItem.MenuItem?.Name ?? "Unknown Item",
            Quantity = orderItem.Quantity,
            Price = orderItem.Price
            // Subtotal is calculated property in DTO
        };
    }
    // --- IMPLEMENT New Methods ---
    public async Task<IEnumerable<OrderSummaryDto>> GetAllOrderSummariesAsync(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null)
    {
        _logger.LogInformation("Fetching all order summaries between {StartDate} and {EndDate}", startDate, endDate);
        var orders = await _orderRepository.GetAllAsync(startDate, endDate);
        return orders.Select(order => MapToOrderSummaryDto(order)); // Reuse existing mapper
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(string periodName, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        _logger.LogInformation("Calculating sales summary for {PeriodName} ({StartDate} to {EndDate})", periodName, startDate, endDate);
        // Use adjusted end date for repo queries to include the whole last day
        var adjustedEndDate = endDate.Date; // Use .Date to get start of day

        var orderCount = await _orderRepository.GetOrderCountAsync(startDate.Date, adjustedEndDate);
        var totalRevenue = await _orderRepository.GetTotalRevenueAsync(startDate.Date, adjustedEndDate);

        return new SalesSummaryDto
        {
            PeriodName = periodName,
            StartDate = startDate.Date,
            EndDate = adjustedEndDate,
            TotalOrders = orderCount,
            TotalRevenue = totalRevenue
        };
    }
    // --- END IMPLEMENT ---


}