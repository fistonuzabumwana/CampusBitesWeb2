// src/CampusBites.Application/Common/Interfaces/IMenuItemRepository.cs
using CampusBites.Domain.Entities; // Needed for MenuItem
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IMenuItemRepository
{
    /// <summary>
    /// Gets a menu item by its unique identifier.
    /// </summary>
    /// <param name="id">The identifier of the menu item.</param>
    /// <returns>The menu item if found; otherwise, null.</returns>
    Task<MenuItem?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all menu items.
    /// </summary>
    /// <returns>A collection of all menu items.</returns>
    Task<IEnumerable<MenuItem>> GetAllAsync();

    /// <summary>
    /// Gets menu items belonging to a specific category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>A collection of menu items in the specified category.</returns>
    Task<IEnumerable<MenuItem>> GetByCategoryAsync(string category);

    /// <summary>
    /// Adds a new menu item to the repository.
    /// </summary>
    /// <param name="menuItem">The menu item to add.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task AddAsync(MenuItem menuItem);

    /// <summary>
    /// Updates an existing menu item in the repository.
    /// </summary>
    /// <param name="menuItem">The menu item with updated information.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task UpdateAsync(MenuItem menuItem);

    /// <summary>
    /// Deletes a menu item from the repository by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the menu item to delete.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    Task DeleteAsync(int id);

    // You might add other methods as needed, e.g.:
    // Task<IEnumerable<MenuItem>> GetAvailableItemsAsync();
    // Task<bool> ExistsAsync(int id);
}