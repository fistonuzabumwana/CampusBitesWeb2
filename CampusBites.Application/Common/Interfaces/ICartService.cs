// src/CampusBites.Application/Common/Interfaces/ICartService.cs
using CampusBites.Domain.Entities;
// No reference to Microsoft.AspNetCore.Http needed here
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

// CORRECTED Interface - Methods do NOT take ISession as a parameter
public interface ICartService
{
    Task AddItemAsync(int menuItemId, int quantity = 1);
    Task RemoveItemAsync(int menuItemId);
    Task<List<CartItem>> GetCartItemsAsync();
    Task<int> GetCartCountAsync();
    Task ClearCartAsync();
    Task UpdateItemQuantityAsync(int menuItemId, int newQuantity); // <<< NEW METHOD

}

