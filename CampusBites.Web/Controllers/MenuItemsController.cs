// src/CampusBites.Web/Controllers/MenuItemsController.cs
using CampusBites.Application.Common.Interfaces; // Needed for IMenuItemService
using CampusBites.Application.DTOs;             // Needed for MenuItemDto
using Microsoft.AspNetCore.Mvc;                 // Needed for ControllerBase, ActionResult, HttpGet etc.
using System.Collections.Generic;
using System.Threading.Tasks;
using System; // For Exception, KeyNotFoundException, ArgumentException


// TODO: Add later for securing endpoints: using Microsoft.AspNetCore.Authorization;

namespace CampusBites.Web.Controllers;

[Route("api/[controller]")] // Sets the base route to "api/menuitems"
[ApiController]             // Enables API-specific behaviors
public class MenuItemsController : ControllerBase // Use ControllerBase for APIs without Views
{
    private readonly IMenuItemService _menuItemService;

    // Inject the application service
    public MenuItemsController(IMenuItemService menuItemService)
    {
        _menuItemService = menuItemService;
    }

    // GET: api/menuitems
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetAllMenuItems()
    {
        var items = await _menuItemService.GetAllMenuItemsAsync();
        return Ok(items); // Returns HTTP 200 OK with the list of items
    }

    // GET: api/menuitems/5
    [HttpGet("{id:int}")] // Route constraint to ensure id is an integer
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemDto>> GetMenuItemById(int id)
    {
        var item = await _menuItemService.GetMenuItemByIdAsync(id);

        if (item == null)
        {
            return NotFound(); // Returns HTTP 404 Not Found
        }

        return Ok(item); // Returns HTTP 200 OK with the item
    }

    // GET: api/menuitems/category/Food
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenuItemsByCategory(string category)
    {
        var items = await _menuItemService.GetMenuItemsByCategoryAsync(category);
        // Consider adding error handling if category is invalid or returns null unexpectedly
        return Ok(items); // Returns HTTP 200 OK with the filtered list
    }

    // Add POST, PUT, DELETE endpoints here later...
    // [HttpPost]
    // public async Task<ActionResult<MenuItemDto>> CreateMenuItem(...) { ... }
    //
    // [HttpPut("{id}")]
    // public async Task<IActionResult> UpdateMenuItem(int id, ...) { ... }
    //
    // [HttpDelete("{id}")]
    // public async Task<IActionResult> DeleteMenuItem(int id) { ... }
    // --- NEW CUD Methods ---

    // POST: api/menuitems
    [HttpPost]
    // TODO: Add later: [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem([FromBody] CreateMenuItemDto createDto)
    {
        // [ApiController] attribute handles basic model state validation automatically
        // returning 400 Bad Request if DTO validation attributes fail.
        // You can add more complex validation here if needed.

        try
        {
            var newItemDto = await _menuItemService.CreateMenuItemAsync(createDto);

            // Return 201 Created with a Location header pointing to the new resource
            // and include the created item (DTO) in the response body.
            return CreatedAtAction(nameof(GetMenuItemById), new { id = newItemDto.Id }, newItemDto);
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error creating menu item."); // Optional logging
            // Return a generic 500 error if something unexpected happens
            return StatusCode(StatusCodes.Status500InternalServerError,
                              new { message = "An error occurred while creating the item.", details = ex.Message });
        }
    }

    // PUT: api/menuitems/5
    [HttpPut("{id:int}")]
    // TODO: Add later: [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)] // Success, no content to return
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // ID mismatch or validation error
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Item not found
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] UpdateMenuItemDto updateDto)
    {
        // Basic check for ID mismatch
        if (id != updateDto.Id)
        {
            return BadRequest(new { message = "ID in URL must match ID in request body." });
        }

        // [ApiController] handles DTO validation attributes

        try
        {
            await _menuItemService.UpdateMenuItemAsync(id, updateDto);
            return NoContent(); // Standard successful PUT response
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message }); // Return 404 if service indicated item not found
        }
        catch (ArgumentException ex) // Catch potential ID mismatch from service layer if added there
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error updating menu item with ID {MenuItemId}", id); // Optional logging
            return StatusCode(StatusCodes.Status500InternalServerError,
                              new { message = "An error occurred while updating the item.", details = ex.Message });
        }
    }

    // DELETE: api/menuitems/5
    [HttpDelete("{id:int}")]
    // TODO: Add later: [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)] // Success, no content to return
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Item not found
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        try
        {
            await _menuItemService.DeleteMenuItemAsync(id);
            return NoContent(); // Standard successful DELETE response
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message }); // Return 404 if service indicated item not found
        }
        catch (Exception ex)
        {
            // _logger.LogError(ex, "Error deleting menu item with ID {MenuItemId}", id); // Optional logging
            return StatusCode(StatusCodes.Status500InternalServerError,
                              new { message = "An error occurred while deleting the item.", details = ex.Message });
        }
    }


    // Add this method to your MenuItemsController class
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetFeaturedMenuItems()
    {
        try
        {
            // Get 3 random available menu items
            var items = await _menuItemService.GetFeaturedMenuItemsAsync();
            return Ok(items);
        }
        catch (Exception ex)
        {
            // Log the error if you have logging configured
            // _logger.LogError(ex, "Error fetching featured menu items");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while fetching featured items.", details = ex.Message });
        }
    }


}