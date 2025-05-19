// src/CampusBites.Application/Services/CartService.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Domain.Entities;
using CampusBites.Application.Common.Extensions; // Correct namespace for SessionExtensions
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.IO; // Required for Path.GetFileName
using System.Threading.Tasks;

namespace CampusBites.Application.Services;

public class CartService : ICartService
{
    // --- Fields ---
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMenuItemRepository _menuItemRepository;
    private const string CartSessionKey = "ShoppingCart"; // Define the constant key

    // --- Constructor (Inject dependencies) ---
    public CartService(IHttpContextAccessor httpContextAccessor, IMenuItemRepository menuItemRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _menuItemRepository = menuItemRepository;
    }

    // --- Session Property (Throws if HttpContext or Session is null) ---
    private ISession Session => _httpContextAccessor.HttpContext?.Session ??
                                throw new InvalidOperationException("HttpContext or Session is not available.");

    // --- Method Implementations ---
    public async Task AddItemAsync(int menuItemId, int quantity = 1)
    {
        var cart = Session.Get<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        var cartItem = cart.FirstOrDefault(item => item.MenuItemId == menuItemId);

        if (cartItem != null)
        {
            cartItem.Quantity += quantity;
        }
        else
        {
            // Use the injected repository field
            var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
            if (menuItem != null && menuItem.IsAvailable)
                {

                string? displayImageUrl = null;
                if (!string.IsNullOrEmpty(menuItem.ImageUrl)) // menuItem.ImageUrl is like /uploads/menu-items/guid.jpg
                {
                    // Transform the URL to point to the FilesController, similar to MenuItemService.MapToDto
                    var fileName = Path.GetFileName(menuItem.ImageUrl);
                    displayImageUrl = $"/api/files/menu-item-image/{fileName}";
                }

                cart.Add(new CartItem
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Price = menuItem.Price,
                    ImageUrl = displayImageUrl,
                    Category = menuItem.Category, // <<< ADD THIS LINE
                    Quantity = quantity
                });
            }
            else
            {
                // Optional: Throw exception if item not found/available to prevent adding
                throw new KeyNotFoundException($"Menu item with ID {menuItemId} not found or is unavailable.");
            }
        }
        Session.Set(CartSessionKey, cart);
    }

    public Task RemoveItemAsync(int menuItemId)
    {
        var cart = Session.Get<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        var cartItem = cart.FirstOrDefault(item => item.MenuItemId == menuItemId);

        if (cartItem != null)
        {
            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
            }
            else
            {
                cart.Remove(cartItem);
            }
            Session.Set(CartSessionKey, cart);
        }
        return Task.CompletedTask;
    }

    public Task<List<CartItem>> GetCartItemsAsync()
    {
        var cart = Session.Get<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        return Task.FromResult(cart);
    }

    public Task<int> GetCartCountAsync()
    {
        var cart = Session.Get<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        return Task.FromResult(cart.Sum(item => item.Quantity));
    }

    public Task ClearCartAsync()
    {
        Session.Remove(CartSessionKey);
        return Task.CompletedTask;
    }


    public async Task UpdateItemQuantityAsync(int menuItemId, int newQuantity)
    {
        var cart = Session.Get<List<CartItem>>(CartSessionKey) ?? new List<CartItem>();
        var cartItem = cart.FirstOrDefault(item => item.MenuItemId == menuItemId);

        if (newQuantity <= 0) // If new quantity is 0 or less, remove the item
        {
            if (cartItem != null)
            {
                cart.Remove(cartItem);
            }
        }
        else // New quantity is positive
        {
            if (cartItem != null)
            {
                // Item exists, update its quantity and ensure price is current
                var menuItemEntity = await _menuItemRepository.GetByIdAsync(menuItemId);
                if (menuItemEntity != null && menuItemEntity.IsAvailable)
                {
                    cartItem.Quantity = newQuantity;
                    cartItem.Price = menuItemEntity.Price; // Update price in case it changed
                    // Ensure other details like Name, ImageUrl, Category are consistent if needed,
                    // but they are usually set when item is first added by AddItemAsync.
                }
                else // Original menu item not found or unavailable, remove from cart
                {
                    cart.Remove(cartItem);
                }
            }
            else
            {
                // Item was not in cart, but a positive quantity was specified. Add it.
                // This reuses the AddItemAsync logic which handles fetching all details.
                // Note: AddItemAsync will add 'newQuantity', not just 1.
                // We need to ensure AddItemAsync correctly uses the passed quantity
                // or we adjust the call here.
                // Let's assume AddItemAsync is: AddItemAsync(menuItemId, quantityToSet)
                // If AddItemAsync always adds its own 'quantity' param to existing or sets it for new,
                // this will effectively set the quantity.
                // Re-checking AddItemAsync structure... it adds the passed quantity if new,
                // or adds to existing quantity. This isn't quite "set to newQuantity".
                // So, it's better to handle adding new explicitly here if that's the desired behavior for Update.
                // For now, let's stick to updating existing or removing.
                // If the requirement is to add if not exist, then:
                // var menuItemEntity = await _menuItemRepository.GetByIdAsync(menuItemId);
                // if (menuItemEntity != null && menuItemEntity.IsAvailable) { /* ... call AddItemAsync logic ... */ }

                // Simpler: If the goal is to update *only if exists*, then do nothing if cartItem is null.
                // If the goal is that setting a quantity for a non-existent item adds it,
                // then you'd call your AddItemAsync(menuItemId, newQuantity) here.
                // Let's assume for "UpdateQuantity" it means the item must be in the cart.
                // If not, the user should use "Add to cart" from menu.
                // If it *should* add it, the most robust way would be to call AddItemAsync
                // but we need to be careful because AddItemAsync itself modifies quantity.
                // A dedicated method to set quantity would be cleaner.

                // Let's refine: if item is not in cart, and newQuantity > 0, we will add it.
                var menuItemEntity = await _menuItemRepository.GetByIdAsync(menuItemId);
                if (menuItemEntity != null && menuItemEntity.IsAvailable)
                {
                    string? displayImageUrl = null;
                    if (!string.IsNullOrEmpty(menuItemEntity.ImageUrl))
                    {
                        var fileName = Path.GetFileName(menuItemEntity.ImageUrl);
                        displayImageUrl = $"/api/files/menu-item-image/{fileName}";
                    }
                    cart.Add(new CartItem
                    {
                        MenuItemId = menuItemEntity.Id,
                        Name = menuItemEntity.Name,
                        Price = menuItemEntity.Price,
                        ImageUrl = displayImageUrl,
                        Category = menuItemEntity.Category,
                        Quantity = newQuantity // Set to the new quantity directly
                    });
                }
            }
        }
        Session.Set(CartSessionKey, cart);
    }
}