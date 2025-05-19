// src/CampusBites.Infrastructure/Persistence/ApplicationDbContext.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Domain.Entities; // Includes Order, OrderItem, Address, MenuItem
using CampusBites.Domain.Enums;   // Includes OrderStatus
using CampusBites.Infrastructure.Identity;
using CampusBites.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection; // Needed if using ApplyConfigurationsFromAssembly

namespace CampusBites.Infrastructure.Persistence;

// Inherit from IdentityDbContext AND implement IApplicationDbContext
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly IConfiguration _configuration; // Add field

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration; // Assign
    }

    // DbSets for Domain Entities
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>(); // <-- ADD THIS




    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Keep this for Identity tables

        // --- Get DB Encryption Key ---
        var dbEncryptionKeySetting = "DbFieldEncryptionSettings:AESKey";
        string base64DbKey = _configuration[dbEncryptionKeySetting]
                           ?? throw new InvalidOperationException($"'{dbEncryptionKeySetting}' not found in configuration.");
        byte[] dbKeyBytes;
        try { dbKeyBytes = Convert.FromBase64String(base64DbKey); } catch { /* Handle error */ throw; }
        if (dbKeyBytes.Length != 32) throw new InvalidOperationException($"DB Key '{dbEncryptionKeySetting}' has invalid length.");

        // Create instance of the converter with the key
        var encryptionConverter = new EncryptionValueConverter(dbKeyBytes);
        // --- End Get Key ---

        // Apply configurations defined in this assembly (if using IEntityTypeConfiguration)
        // builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // --- MenuItem Configuration ---
        builder.Entity<MenuItem>(menuItem =>
        {
            menuItem.Property(m => m.Name)
                .HasMaxLength(200)
                .IsRequired();

            menuItem.Property(p => p.Price)
                   .HasColumnType("decimal(18,2)");
        });

        // --- Order Configuration ---
        // --- Order Configuration ---
        builder.Entity<Order>(order =>
        {
            order.Property(o => o.OrderTotal).HasColumnType("decimal(18,2)");
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);

            // Relationship: Order -> OrderItems (One-to-Many)
            order.HasMany(o => o.OrderItems)      // Navigation property on Order
                 .WithOne()                       // No navigation property back on OrderItem (change if you add one)
                 .HasForeignKey(oi => oi.OrderId) // Foreign key on OrderItem
                 .OnDelete(DeleteBehavior.Cascade);

            // Relationship: Order -> Shipping Address (Many-to-One)
            order.HasOne(o => o.ShippingAddress) // Navigation property on Order
                 .WithMany()                     // No collection navigation property back on Address
                 .HasForeignKey(o => o.ShippingAddressId) // Foreign key on Order
                 .OnDelete(DeleteBehavior.Restrict); // Prevent deleting Address if used

            // Relationship: Order -> Billing Address (Many-to-One)
            order.HasOne(o => o.BillingAddress)  // Navigation property on Order
                 .WithMany()                     // No collection navigation property back on Address
                 .HasForeignKey(o => o.BillingAddressId) // Foreign key on Order
                 .OnDelete(DeleteBehavior.Restrict); // Prevent deleting Address if used

            // Relationship Order -> User (IdentityUser) is handled by convention/IdentityDbContext
        });

        // --- OrderItem Configuration ---
        builder.Entity<OrderItem>(orderItem =>
        {
            orderItem.Property(oi => oi.Price).HasColumnType("decimal(18,2)");

            // Relationship: OrderItem -> MenuItem (Many-to-One)
            orderItem.HasOne(oi => oi.MenuItem) // Navigation property on OrderItem
                     .WithMany()                // No collection navigation property back on MenuItem
                     .HasForeignKey(oi => oi.MenuItemId) // Foreign key on OrderItem
                     .OnDelete(DeleteBehavior.Restrict); // Prevent deleting MenuItem if used
        });

        // --- Address Configuration ---
        builder.Entity<Address>(address =>
        {
            // Apply the converter to the StreetAddress property
            address.Property(a => a.StreetAddress)
                   .HasConversion(encryptionConverter)
                   // Optionally specify max length for encrypted string (Base64 is ~33% larger + IV/Tag)
                   // .HasMaxLength(500); // Adjust as needed
                   ;

            // Ensure other Address properties are configured if needed
            address.Property(a => a.Sector).HasMaxLength(100).IsRequired();
            address.Property(a => a.District).HasMaxLength(100).IsRequired();
            // address.Property(a => a.Country)... if added back
            // Relationship Address -> User (IdentityUser) is handled by convention/IdentityDbContext
            // We don't need to define the Order relationships here as they are defined from the Order side.
        });


        // --- Optional: AuditLog Configuration ---
        builder.Entity<AuditLog>(audit =>
        {
            audit.HasIndex(a => a.Timestamp); // Index common query fields
            audit.HasIndex(a => a.UserId);
            audit.HasIndex(a => a.Action);
            audit.Property(a => a.Details).HasMaxLength(2000); // Example max length
        });
        // --- END Optional ---

        builder.Entity<ApplicationUser>(user => {
            user.Property(u => u.FirstName).HasMaxLength(100);
            user.Property(u => u.LastName).HasMaxLength(100);
            user.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
        });
    }
    // SaveChangesAsync is inherited from DbContext
}