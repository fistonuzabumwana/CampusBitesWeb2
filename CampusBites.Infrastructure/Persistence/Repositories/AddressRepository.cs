// src/CampusBites.Infrastructure/Persistence/Repositories/AddressRepository.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Domain.Entities;
using System.Threading.Tasks;

namespace CampusBites.Infrastructure.Persistence.Repositories;

public class AddressRepository : IAddressRepository
{
    private readonly IApplicationDbContext _context;

    public AddressRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Address> AddAsync(Address address)
    {
        // Add the entity to the DbContext's change tracker
        // Note: Add is synchronous, not AddAsync for just adding to tracker
        _context.Addresses.Add(address);

        // The entity passed in will have its ID populated after SaveChangesAsync
        // is called elsewhere (e.g., in the OrderService).
        // Return Task.FromResult as the interface requires a Task<Address>.
        return Task.FromResult(address);
    }

    // Implement GetByIdAsync, GetByUserIdAsync etc. later if needed
    // public async Task<Address?> GetByIdAsync(int id)
    // {
    //     return await _context.Addresses.FindAsync(id);
    // }
    //
    // public async Task<IEnumerable<Address>> GetByUserIdAsync(string userId)
    // {
    //     return await _context.Addresses
    //         .Where(a => a.UserId == userId)
    //         .ToListAsync();
    // }
}