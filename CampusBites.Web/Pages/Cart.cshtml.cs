// src/CampusBites.Web/Pages/Cart.cshtml.cs
using CampusBites.Application.Common.Interfaces; // For ICartService
using CampusBites.Domain.Entities;             // For CartItem
using Microsoft.AspNetCore.Mvc;                  // For IActionResult, RedirectToPage
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;                               // For Sum()
using System.Threading.Tasks;

namespace CampusBites.Web.Pages;

public class CartModel : PageModel
{
    private readonly ICartService _cartService;

    // Property to hold the items displayed on the page
    public List<CartItem> CartItems { get; private set; } = new List<CartItem>();
    // Property to hold the calculated total
    public decimal CartTotal { get; private set; }

    // Inject the cart service
    public CartModel(ICartService cartService)
    {
        _cartService = cartService;
    }

    // Handles GET requests for the page
    public async Task OnGetAsync()
    {
        // Get items from the service (which reads from session)
        CartItems = await _cartService.GetCartItemsAsync();
        // Calculate the total price
        CartTotal = CartItems.Sum(item => item.Price * item.Quantity);
    }

    // Handles POST requests specifically for removing an item
    // The 'id' parameter name matches the asp-route-id in the form
    public async Task<IActionResult> OnPostRemoveItemAsync(int id)
    {
        // Call the service to remove (or decrease quantity)
        await _cartService.RemoveItemAsync(id);
        // Redirect back to the Cart page to refresh the display
        return RedirectToPage();
    }

    // Handles POST requests specifically for clearing the cart
    public async Task<IActionResult> OnPostClearCartAsync()
    {
        await _cartService.ClearCartAsync();
        return RedirectToPage();
    }

    // NEW HANDLER for the "-" button
    // Parameter name 'id' matches asp-route-id
    public async Task<IActionResult> OnPostDecreaseQuantityAsync(int id)
    {
        // Your CartService.RemoveItemAsync already handles decreasing quantity by 1
        // or removing the item if its quantity was 1.
        await _cartService.RemoveItemAsync(id); // 'id' here is the menuItemId
        return RedirectToPage();
    }

    // NEW HANDLER for the "+" button
    // Parameter name 'id' matches asp-route-id
    public async Task<IActionResult> OnPostIncreaseQuantityAsync(int id)
    {
        // Your CartService.AddItemAsync with a quantity of 1 will
        // increment the quantity if the item already exists in the cart.
        await _cartService.AddItemAsync(id, 1); // 'id' here is the menuItemId
        return RedirectToPage();
    }


    public async Task<IActionResult> OnPostUpdateQuantityAsync(int id, int newQuantity)
    {
        // The min="0" on the input field provides client-side help,
        // but server-side validation is still good.
        if (newQuantity < 0)
        {
            // Optionally, add a ModelState error or a TempData message
            // For now, just redirecting effectively ignores invalid negative input not caught by client.
            return RedirectToPage();
        }

        await _cartService.UpdateItemQuantityAsync(id, newQuantity); // 'id' is menuItemId
        return RedirectToPage();
    }
}