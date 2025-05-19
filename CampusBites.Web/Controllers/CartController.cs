// src/CampusBites.Web/Controllers/CartController.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.DTOs;
using CampusBites.Domain.Entities; // For CartItem return type if needed
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusBites.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    // POST: api/cart/add/5
    [HttpPost("add/{menuItemId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)] // Returning new count
    [ProducesResponseType(StatusCodes.Status404NotFound)] // If menu item not found/available
    public async Task<IActionResult> AddItem(int menuItemId, [FromBody] AddToCartRequest? request)
    {
        try
        {
            int quantity = request?.Quantity > 0 ? request.Quantity : 1; // Use quantity from body or default to 1
            await _cartService.AddItemAsync(menuItemId, quantity);
            var newCount = await _cartService.GetCartCountAsync();
            return Ok(new { newCount = newCount }); // Return the new total quantity in cart
        }
        catch (KeyNotFoundException ex) // Example: Catch specific exception if service throws it when item not found
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex) // General error handling
        {
            // Log the error ex
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while adding the item. " + ex.Message,  });
        }
    }

    // GET: api/cart/count
    [HttpGet("count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCartCount()
    {
        var count = await _cartService.GetCartCountAsync();
        return Ok(new { count = count });
    }

    // GET: api/cart
    [HttpGet]
    [ProducesResponseType(typeof(List<CartItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CartItem>>> GetCartItems()
    {
        var items = await _cartService.GetCartItemsAsync();
        return Ok(items);
    }


    // POST: api/cart/remove/5
    [HttpPost("remove/{menuItemId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(int menuItemId)
    {
        await _cartService.RemoveItemAsync(menuItemId); // Assumes decrease or removal
        var newCount = await _cartService.GetCartCountAsync();
        return Ok(new { newCount = newCount });
    }

    // POST: api/cart/clear
    [HttpPost("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync();
        return NoContent(); // Indicate success with no content
    }
}