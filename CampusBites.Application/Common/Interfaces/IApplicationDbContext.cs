// src/CampusBites.Application/Common/Interfaces/IApplicationDbContext.cs
using CampusBites.Domain.Entities; // We'll create this entity next
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Add DbSets for your Domain Entities here
    DbSet<MenuItem> MenuItems { get; } // Example for MenuItem

    // --- ADD New DbSets ---
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Address> Addresses { get; }

    DbSet<AuditLog> AuditLogs { get; } // <-- ADD THIS

    // --- END ADD ---
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    // --- ADD DATABASE PROPERTY ---
    DatabaseFacade Database { get; }
    // --- END ADD ---

    // --- ADD ENTRY METHOD SIGNATURE ---
    /// <summary>
    /// Gets an EntityEntry<TEntity> for the given entity.
    /// The entry provides access to change tracking information and operations for the entity.
    /// </summary>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    // --- END ADD ---
}