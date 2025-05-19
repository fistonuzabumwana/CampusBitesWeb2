// src/CampusBites.Web/Pages/Checkout.cshtml.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Domain.Entities;
using CampusBites.Web.ViewModels; // For AddressViewModel
using Microsoft.AspNetCore.Authorization; // For securing the page
using Microsoft.AspNetCore.Identity; // For UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System; // For ArgumentNullException etc.
using System.ComponentModel.DataAnnotations;
using CampusBites.Domain.Enums;
using CampusBites.Infrastructure.Identity; // For validation attributes


namespace CampusBites.Web.Pages;

[Authorize] // Require users to be logged in to checkout
public class CheckoutModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly IOrderService _orderService;
    private readonly UserManager<ApplicationUser> _userManager;
    // Note: AddressRepository isn't directly needed if OrderService handles address saving

    public List<CartItem> CartItems { get; set; } = new List<CartItem>();
    public decimal CartTotal { get; set; }

    // Bind properties for the forms
    [BindProperty]
    public AddressViewModel ShippingAddress { get; set; } = new AddressViewModel();

    [Display(Name = "Billing address same as shipping")]
    public bool IsSameAsShipping { get; set; } = true; // Default to checked

    [BindProperty]
    public AddressViewModel BillingAddress { get; set; } = new AddressViewModel();

    // --- NEW: Payment Binding Properties ---
    [BindProperty]
    [Required(ErrorMessage = "Please select a payment method.")]
    public string SelectedPaymentMethod { get; set; } = "MTNMoMo"; // Default to MTN

    [BindProperty]
    [Required(ErrorMessage = "Phone number is required for Mobile Money.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")] // Basic phone validation
    [Display(Name = "Phone Number")]
    public string PaymentPhoneNumber { get; set; } = string.Empty;
    // --- END NEW ---


    // TODO: Add property and logic for "Use shipping address for billing" checkbox later

    [TempData]
    public string? ErrorMessage { get; set; }


    public CheckoutModel(
        ICartService cartService,
        IOrderService orderService,
        UserManager<ApplicationUser> userManager)
    {
        _cartService = cartService;
        _orderService = orderService;
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CartItems = await _cartService.GetCartItemsAsync();
        if (!CartItems.Any())
        {
            // Redirect to cart page or menu page if cart is empty
            TempData["Message"] = "Your cart is empty.";
            return RedirectToPage("/Cart");
        }
        CartTotal = CartItems.Sum(item => item.Price * item.Quantity);

        // Pre-fill billing if shipping is potentially pre-filled and checkbox is checked
        if (IsSameAsShipping && ShippingAddress != null)
        {
            BillingAddress = new AddressViewModel // Create new instance
            {
                StreetAddress = ShippingAddress.StreetAddress,
                Sector = ShippingAddress.Sector,
                District = ShippingAddress.District
            };
        }

        // TODO: Pre-populate address fields if user has saved addresses (future enhancement)

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Get cart items again to ensure they haven't changed or emptied
        CartItems = await _cartService.GetCartItemsAsync();
        if (!CartItems.Any())
        {
            ModelState.AddModelError(string.Empty, "Your cart is empty. Please add items before placing an order.");
            // Recalculate total in case needed by view on return
            CartTotal = CartItems.Sum(item => item.Price * item.Quantity);
            return Page();
        }

        // Recalculate total for display if returning the page due to errors
        CartTotal = CartItems.Sum(item => item.Price * item.Quantity);


        // Handle the "Same as Shipping" logic server-side
        if (IsSameAsShipping)
        {
            // If checked, we ignore billing address validation errors
            // because the fields were likely hidden and auto-filled by JS.
            // We also copy the shipping address over server-side for processing.
            ModelState.Remove("BillingAddress.StreetAddress");
            ModelState.Remove("BillingAddress.Sector");
            ModelState.Remove("BillingAddress.District");

            BillingAddress = new AddressViewModel // Use a new instance
            {
                StreetAddress = ShippingAddress.StreetAddress,
                Sector = ShippingAddress.Sector,
                District = ShippingAddress.District
            };
        }
        // If IsSameAsShipping is false, the BillingAddress property bound from
        // the visible form fields will be used and validated normally.


        if (!ModelState.IsValid)
        {
            // If AddressViewModel validation fails, return the page
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // Should not happen if [Authorize] is working, but good check
            return Challenge(); // Force login
        }

        // Map ViewModels to Domain Entities
        var shippingAddressEntity = new Address
        {
            StreetAddress = ShippingAddress.StreetAddress,
            Sector = ShippingAddress.Sector,
            District = ShippingAddress.District,
            // Country = ShippingAddress.Country, // Add if using Country
            UserId = user.Id // Link address to user
        };

        var billingAddressEntity = new Address
        {
            StreetAddress = BillingAddress.StreetAddress,
            Sector = BillingAddress.Sector,
            District = BillingAddress.District,
            // Country = BillingAddress.Country, // Add if using Country
            UserId = user.Id // Link address to user
        };

        // Prepare order data including payment info
        var orderData = new OrderCreationData // Use a temporary structure or pass directly
        {
            UserId = user.Id,
            CartItems = CartItems,
            ShippingAddress = shippingAddressEntity,
            BillingAddress = billingAddressEntity,
            // --- ADD Payment Info ---
            PaymentMethod = this.SelectedPaymentMethod,
            PaymentReference = this.PaymentPhoneNumber
            // --- END ADD ---
        };


        try
        {
            // Call the order service to create the order
            int orderId = await _orderService.CreateOrderAsync(
                orderData.UserId,
                 orderData.CartItems,
                 orderData.ShippingAddress,
                 orderData.BillingAddress,
                 orderData.PaymentMethod,  // Pass new info
                 orderData.PaymentReference // Pass new info
            );

            bool statusUpdated = await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Processing);


            // Clear the cart after successful order placement
            await _cartService.ClearCartAsync();

            // Redirect to an Order Confirmation page
            return RedirectToPage("/OrderConfirmation", new { orderId = orderId });

        }
        catch (ArgumentException argEx) { ModelState.AddModelError(string.Empty, $"Order placement failed: {argEx.Message}"); }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, $"An unexpected error... ({ex.Message})"); }
        return Page();

    }
    // Helper structure (optional, could pass params directly)
    private class OrderCreationData
    {
        public string UserId { get; set; } = string.Empty;
        public List<CartItem> CartItems { get; set; } = new();
        public Address ShippingAddress { get; set; } = default!;
        public Address BillingAddress { get; set; } = default!;
        public string? PaymentMethod { get; set; }
        public string? PaymentReference { get; set; }
    }
}