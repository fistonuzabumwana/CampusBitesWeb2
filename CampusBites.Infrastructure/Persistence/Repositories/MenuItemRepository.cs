// src/CampusBites.Infrastructure/Persistence/Repositories/MenuItemRepository.cs
using CampusBites.Application.Common.Interfaces; // Needed for IApplicationDbContext & IMenuItemRepository
using CampusBites.Domain.Entities;             // Needed for MenuItem
using Microsoft.EntityFrameworkCore;             // Needed for EF Core methods like ToListAsync, FindAsync etc.
using System.Collections.Generic;
using System.Linq; // Needed for Where
using System.Threading.Tasks;

namespace CampusBites.Infrastructure.Persistence.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly IApplicationDbContext _context;

    // Inject the DbContext via the interface
    public MenuItemRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MenuItem?> GetByIdAsync(int id)
    {
        // FindAsync is efficient for finding by primary key
        return await _context.MenuItems.FindAsync(id);
    }

    public async Task<IEnumerable<MenuItem>> GetAllAsync()
    {
        // Get all items and return as a List (or other IEnumerable)
        return await _context.MenuItems.ToListAsync();
    }

    public async Task<IEnumerable<MenuItem>> GetByCategoryAsync(string category)
    {
        // Filter items by category using Where
        return await _context.MenuItems
            .Where(item => item.Category == category)
            .ToListAsync();
    }

    public async Task AddAsync(MenuItem menuItem)
    {
        // Add the new entity to the DbSet
        await _context.MenuItems.AddAsync(menuItem);
        // IMPORTANT: The repository doesn't usually call SaveChangesAsync.
        // The Unit of Work pattern (often handled by the DbContext itself
        // when managed by a service layer or middleware) typically saves changes.
        // For now, we assume SaveChangesAsync will be called elsewhere after this operation.
    }

    public Task UpdateAsync(MenuItem menuItem)
    {
        // EF Core tracks changes. Simply marking the entity as Modified tells EF
        // to generate an UPDATE statement when SaveChangesAsync is called.
        // Note: This assumes the menuItem passed in is potentially detached.
        // If it's already tracked, changing its properties is enough.
        // Using Entry().State is safer for potentially detached entities.
        _context.MenuItems.Entry(menuItem).State = EntityState.Modified;

        // We still don't call SaveChangesAsync here. Return Task.CompletedTask
        // as the interface requires a Task, but the modification is synchronous
        // until SaveChangesAsync is called.
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        // First, find the item to delete
        var menuItemToDelete = await _context.MenuItems.FindAsync(id);

        if (menuItemToDelete != null)
        {
            // Remove the entity from the DbSet
            _context.MenuItems.Remove(menuItemToDelete);
            // Again, SaveChangesAsync is expected to be called elsewhere.
        }
        // If not found, we might throw an exception or just do nothing,
        // depending on requirements. Here, we do nothing if not found.
    }
}