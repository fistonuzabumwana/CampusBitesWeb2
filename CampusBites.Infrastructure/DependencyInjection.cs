// src/CampusBites.Infrastructure/DependencyInjection.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Options;
using CampusBites.Application.Common.Security;
using CampusBites.Infrastructure.Identity;
using CampusBites.Infrastructure.Persistence;
using CampusBites.Infrastructure.Persistence.Repositories; // <= Add this using
using CampusBites.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusBites.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Use UseNpgsql for PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Register ApplicationDbContext also as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Configure Identity
        // Ensure necessary Identity packages (like .UI) are installed if AddDefaultIdentity isn't found
        services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
             .AddRoles<IdentityRole>() // <--- ADD THIS METHOD CALL
             .AddEntityFrameworkStores<ApplicationDbContext>();
        // --- Configure Options ---
        services.Configure<MailSettings>(configuration.GetSection("MailSettings"));
        // Add other options if needed later
        // --- End Configure Options ---

        // --- REGISTER REPOSITORIES ---
        // ... (IMenuItemRepository, IAddressRepository, IOrderRepository) ...

        // --- REGISTER OTHER INFRASTRUCTURE SERVICES ---
        services.AddScoped<IFileStorageService, LocalStorageService>();
        services.AddTransient<IEmailSender, EmailSender>(); // Use Transient for email sender
        // --- END REGISTER OTHER ---

        // --- REGISTER AUTHORIZATION POLICIES ---
        services.AddAuthorization(options =>
        {
            // Policy for Viewing Menu Items (Admin Section)
            options.AddPolicy(Permissions.MenuItems.View, policy =>
                policy.RequireClaim("permission", Permissions.MenuItems.View));

            // Policy for Creating Menu Items
            options.AddPolicy(Permissions.MenuItems.Create, policy =>
                policy.RequireClaim("permission", Permissions.MenuItems.Create));

            // Policy for Editing Menu Items
            options.AddPolicy(Permissions.MenuItems.Edit, policy =>
                policy.RequireClaim("permission", Permissions.MenuItems.Edit));

            // Policy for Deleting Menu Items
            options.AddPolicy(Permissions.MenuItems.Delete, policy =>
                policy.RequireClaim("permission", Permissions.MenuItems.Delete));

            // --- Add Policies for other modules as needed ---

            // Example: Users
            options.AddPolicy(Permissions.Users.View, policy =>
                 policy.RequireClaim("permission", Permissions.Users.View));
            options.AddPolicy(Permissions.Users.Edit, policy =>
                 policy.RequireClaim("permission", Permissions.Users.Edit));
            options.AddPolicy(Permissions.Users.ManageRoles, policy =>
                policy.RequireClaim("permission", Permissions.Users.ManageRoles));

            // Example: Orders
            options.AddPolicy(Permissions.Orders.ViewAll, policy =>
                policy.RequireClaim("permission", Permissions.Orders.ViewAll));
            options.AddPolicy(Permissions.Orders.EditStatus, policy =>
                policy.RequireClaim("permission", Permissions.Orders.EditStatus));

            // Example: Reports
            options.AddPolicy(Permissions.Reports.ViewSales, policy =>
                 policy.RequireClaim("permission", Permissions.Reports.ViewSales));
            options.AddPolicy(Permissions.Reports.ViewUsage, policy =>
                 policy.RequireClaim("permission", Permissions.Reports.ViewUsage));

            // You could also create policies requiring *multiple* claims or roles
            // options.AddPolicy("CanManageEverythingImportant", policy =>
            //     policy.RequireRole("Admin").RequireClaim("permission", Permissions.Critical.DoAnything));
        });
        // --- END REGISTER AUTHORIZATION POLICIES ---

        // --- ADD THIS LINE TO REGISTER THE REPOSITORY ---
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>(); // <-- ADD THIS LINE
        services.AddScoped<IOrderRepository, OrderRepository>();   // <-- ADD THIS LINE
        // --- END OF LINE TO ADD ---

        // Register other Infrastructure services here later (e.g., EmailSender, FileStorage)
        // services.AddTransient<IEmailSender, EmailSender>();
        // --- ADD FILE STORAGE SERVICE REGISTRATION ---
        services.AddScoped<IFileStorageService, LocalStorageService>();
        // --- END ADD ---

        return services;
    }
}