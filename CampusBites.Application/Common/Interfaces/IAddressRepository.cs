// src/CampusBites.Application/Common/Interfaces/IAddressRepository.cs
using CampusBites.Domain.Entities;
using System.Threading.Tasks;
using System.Collections.Generic; // If adding Get methods later

namespace CampusBites.Application.Common.Interfaces;

public interface IAddressRepository
{
    /// <summary>
    /// Adds a new address to the data store.
    /// </summary>
    /// <param name="address">The address entity to add.</param>
    /// <returns>The added address entity, possibly with updated Id.</returns>
    Task<Address> AddAsync(Address address);

    // Add other methods later if needed:
    // Task<Address?> GetByIdAsync(int id);
    // Task<IEnumerable<Address>> GetByUserIdAsync(string userId);
    // Task UpdateAsync(Address address);
}