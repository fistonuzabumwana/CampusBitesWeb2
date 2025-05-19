// src/CampusBites.Application/Common/Interfaces/IMenuItemService.cs
using CampusBites.Application.DTOs; // Needed for MenuItemDto
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IMenuItemService
{
    /// <summary>
    /// Gets a menu item by its ID, returned as a DTO.
    /// </summary>
    /// <param name="id">The ID of the menu item.</param>
    /// <returns>A MenuItemDto if found; otherwise, null.</returns>
    Task<MenuItemDto?> GetMenuItemByIdAsync(int id);

    /// <summary>
    /// Gets all menu items, returned as DTOs.
    /// </summary>
    /// <returns>A collection of MenuItemDto.</returns>
    Task<IEnumerable<MenuItemDto>> GetAllMenuItemsAsync();

    /// <summary>
    /// Gets menu items by category, returned as DTOs.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>A collection of MenuItemDto in the specified category.</returns>
    Task<IEnumerable<MenuItemDto>> GetMenuItemsByCategoryAsync(string category);

    // Add methods for Create, Update, Delete later if needed.
    // These might accept specific Create/Update DTOs.
    // Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemDto createDto);
    // Task UpdateMenuItemAsync(int id, UpdateMenuItemDto updateDto);
    // Task DeleteMenuItemAsync(int id);

    // --- NEW CRUD Methods ---

    /// <summary>
    /// Creates a new menu item based on the provided DTO.
    /// </summary>
    /// <param name="createDto">Data for the new menu item.</param>
    /// <returns>A DTO representing the newly created menu item (including its ID).</returns>
    Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemDto createDto);

    /// <summary>
    /// Updates an existing menu item.
    /// </summary>
    /// <param name="id">The ID of the menu item to update.</param>
    /// <param name="updateDto">The updated data for the menu item.</param>
    /// <returns>Task representing the asynchronous operation. Throws KeyNotFoundException if item not found.</returns>
    Task UpdateMenuItemAsync(int id, UpdateMenuItemDto updateDto);

    /// <summary>
    /// Deletes a menu item by its ID.
    /// </summary>
    /// <param name="id">The ID of the menu item to delete.</param>
    /// <returns>Task representing the asynchronous operation. Throws KeyNotFoundException if item not found.</returns>
    Task DeleteMenuItemAsync(int id);
    // --- END NEW ---

    Task<IEnumerable<MenuItemDto>> GetFeaturedMenuItemsAsync();
}