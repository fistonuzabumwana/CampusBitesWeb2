// src/CampusBites.Application/Services/MenuItemService.cs
using CampusBites.Application.Common.Interfaces; // For IMenuItemService, IMenuItemRepository
using CampusBites.Application.DTOs;             // For MenuItemDto
using CampusBites.Domain.Entities;             // For MenuItem (needed for mapping)
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;                             // For Select (mapping)
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using AutoMapper;

namespace CampusBites.Application.Services;

public class MenuItemService : IMenuItemService
{
    private readonly IMenuItemRepository _menuItemRepository;
    // Inject other dependencies like logging, mapping libraries later if needed
    private readonly IApplicationDbContext _context; // Inject DbContext for SaveChangesAsync
    private readonly IFileStorageService _fileStorageService; // Inject file service
    private readonly IAuditService _auditService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;  // Add this field



    // private readonly IMapper _mapper;

    public MenuItemService(IMenuItemRepository menuItemRepository, IApplicationDbContext context, IFileStorageService fileStorageService, IAuditService auditService, IHttpContextAccessor httpContextAccessor, IMapper mapper)
    {
        _menuItemRepository = menuItemRepository;
        _context = context;
        _fileStorageService = fileStorageService; // Assign
        _auditService = auditService;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(int id)
    {
        var menuItem = await _menuItemRepository.GetByIdAsync(id);

        if (menuItem == null)
        {
            return null; // Not found
        }

        // Manual Mapping from Entity to DTO
        return MapToDto(menuItem);
    }

    public async Task<IEnumerable<MenuItemDto>> GetAllMenuItemsAsync()
    {
        var menuItems = await _menuItemRepository.GetAllAsync();

        // Manual Mapping using LINQ Select
        return menuItems.Select(menuItem => MapToDto(menuItem));
    }

    public async Task<IEnumerable<MenuItemDto>> GetMenuItemsByCategoryAsync(string category)
    {
        var menuItems = await _menuItemRepository.GetByCategoryAsync(category);

        // Manual Mapping using LINQ Select
        return menuItems.Select(menuItem => MapToDto(menuItem));
    }




    // --- NEW CRUD Methods ---

    public async Task<MenuItemDto> CreateMenuItemAsync(CreateMenuItemDto createDto)
    {
        string? imageUrl = null;
        // Check if a file was uploaded
        if (createDto.ImageFile == null || createDto.ImageFile.Length == 0)
        {
            throw new ArgumentException("Image file is required");
        }
     
            // Use using statement for stream disposal
            using (var stream = createDto.ImageFile.OpenReadStream())
            {
                // Save file and get the relative path
                imageUrl = await _fileStorageService.SaveFileAsync(
                    stream,
                    createDto.ImageFile.FileName,
                    "menu-items" // Specify a container/folder name
                );
            }
        

        var newMenuItem = new MenuItem
        {
            Name = createDto.Name,
            Description = createDto.Description,
            Price = createDto.Price,
            Category = createDto.Category,
            IsAvailable = createDto.IsAvailable,
            ImageUrl = imageUrl // Assign the saved path or null
        };

        await _menuItemRepository.AddAsync(newMenuItem);
        await _context.SaveChangesAsync(CancellationToken.None);

        // --- Log Audit ---
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditService.LogAsync(
            action: "CreateMenuItem",
            userId: userId,
            entityType: nameof(MenuItem),
            entityId: newMenuItem.Id.ToString(), // Get ID after save
            details: $"Created menu item: {newMenuItem.Name}");
        // --- End Log ---

        return MapToDto(newMenuItem);
    }

    public async Task UpdateMenuItemAsync(int id, UpdateMenuItemDto updateDto)
    {
        if (id != updateDto.Id) throw new ArgumentException("ID mismatch.");

        var menuItemToUpdate = await _menuItemRepository.GetByIdAsync(id);
        if (menuItemToUpdate == null) throw new KeyNotFoundException($"Menu item with ID {id} not found.");

        string? oldImagePath = menuItemToUpdate.ImageUrl; // Store old path
        string? newImagePath = oldImagePath; // Assume no change initially

        // Check if a NEW file was uploaded
        if (updateDto.ImageFile != null && updateDto.ImageFile.Length > 0)
        {
            using (var stream = updateDto.ImageFile.OpenReadStream())
            {
                // Save the new file
                newImagePath = await _fileStorageService.SaveFileAsync(
                    stream,
                    updateDto.ImageFile.FileName,
                    "menu-items"
                );
            }
        }

        // Map other properties
        menuItemToUpdate.Name = updateDto.Name;
        menuItemToUpdate.Description = updateDto.Description;
        menuItemToUpdate.Price = updateDto.Price;
        menuItemToUpdate.Category = updateDto.Category;
        menuItemToUpdate.IsAvailable = updateDto.IsAvailable;
        menuItemToUpdate.ImageUrl = newImagePath; // Update path in entity

        await _menuItemRepository.UpdateAsync(menuItemToUpdate);
        await _context.SaveChangesAsync(CancellationToken.None);
         // --- Log Audit ---
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditService.LogAsync(
            action: "CreateMenuItem",
            userId: userId,
            entityType: nameof(MenuItem),
            entityId: updateDto.Id.ToString(), // Get ID after save
            details: $"Created menu item: {updateDto.Name}");
        // --- End Log ---

        // If update was successful AND a new image was uploaded AND an old image existed, delete the old one
        if (newImagePath != oldImagePath && !string.IsNullOrEmpty(oldImagePath))
        {
            await _fileStorageService.DeleteFileAsync(oldImagePath);
        }
    }

    public async Task DeleteMenuItemAsync(int id)
    {
        // 1. Check if item exists in any OrderItems
        //    We need direct DbContext access here for this check.
        bool isInOrders = await _context.OrderItems
                                        .AnyAsync(oi => oi.MenuItemId == id, CancellationToken.None);

        if (isInOrders)
        {
            // Throw a specific exception that the UI layer can catch
            throw new InvalidOperationException($"Cannot delete menu item with ID {id} because it exists in past orders.");
        }
        // 2. Proceed with deletion

        var menuItemToDelete = await _menuItemRepository.GetByIdAsync(id);
        if (menuItemToDelete == null) throw new KeyNotFoundException($"Menu item with ID {id} not found.");

        string? imagePathToDelete = menuItemToDelete.ImageUrl; // Store path before deleting entity record

        await _menuItemRepository.DeleteAsync(id);
        await _context.SaveChangesAsync(CancellationToken.None);
        // --- Log Audit ---
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        await _auditService.LogAsync(
            action: "CreateMenuItem",
            userId: userId,
            entityType: nameof(MenuItem),
            entityId: id.ToString(), // Get ID after save
            details: $"Created menu item: {id}");
        // --- End Log ---

        // If delete from DB was successful AND an image path existed, delete the file
        if (!string.IsNullOrEmpty(imagePathToDelete))
        {
            await _fileStorageService.DeleteFileAsync(imagePathToDelete);
        }
    }

    // --- Private Helper Method for Manual Mapping ---
    // (Consider using AutoMapper or Mapster for more complex scenarios)
    private static MenuItemDto MapToDto(MenuItem menuItem)
    {
        string? displayImageUrl = null;
        if (!string.IsNullOrEmpty(menuItem.ImageUrl))
        {
            // ImageUrl from DB is like: /uploads/menu-items/some-guid.jpg
            // Extract the filename part
            var fileName = Path.GetFileName(menuItem.ImageUrl);
            // Construct URL to the new API endpoint
            displayImageUrl = $"/api/files/menu-item-image/{fileName}";
        }

        return new MenuItemDto
        {
            Id = menuItem.Id,
            Name = menuItem.Name,
            Description = menuItem.Description,
            Price = menuItem.Price,
            ImageUrl = displayImageUrl, // Use the URL pointing to the FilesController action
            Category = menuItem.Category,
            IsAvailable = menuItem.IsAvailable
        };
    }

    // Implement Create/Update/Delete methods here later...

    public async Task<IEnumerable<MenuItemDto>> GetFeaturedMenuItemsAsync()
    {
        // Get 3 random available menu items
        var randomItems = await _context.MenuItems
            .Where(m => m.IsAvailable)
            .OrderBy(r => Guid.NewGuid())
            .Take(3)
            .Select(m => _mapper.Map<MenuItemDto>(m))
            .ToListAsync();

        return randomItems;
    }
}