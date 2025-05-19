// src/CampusBites.Infrastructure/Identity/ApplicationDbInitializer.cs
using CampusBites.Application.Common.Security; // <-- Add this using
using CampusBites.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // <-- Add this using
using System.Linq;
using System.Security.Claims; // <-- Add this using
using System.Threading.Tasks;


namespace CampusBites.Infrastructure.Identity;

public static class ApplicationDbInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        using (var scope = scopeFactory.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var loggerFactory = scopedServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("CampusBites.Infrastructure.Identity.ApplicationDbInitializer");

            try
            {
                var context = scopedServices.GetRequiredService<ApplicationDbContext>();
                var userManager = scopedServices.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scopedServices.GetRequiredService<RoleManager<IdentityRole>>();

                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();

                logger.LogInformation("Seeding roles...");
                await SeedRolesAsync(roleManager, logger); // Ensure roles exist first

                logger.LogInformation("Seeding default admin user and permissions...");
                await SeedAdminUserAndPermissionsAsync(userManager, roleManager, logger); // Updated method call

                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database.");
                throw;
            }
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string[] roleNames = { Roles.Admin, Roles.User }; // Use constants if defined, e.g., Roles.Admin
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (roleResult.Succeeded) { logger.LogInformation("Role '{RoleName}' created successfully.", roleName); }
                else { logger.LogError("Error creating role '{RoleName}'. Errors: {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description))); }
            }
        }
    }

    // Renamed method for clarity
    private static async Task SeedAdminUserAndPermissionsAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        string adminEmail = "admin@campusbites.local"; // CHANGE THIS
        string adminPassword = "AdminPassword123!";   // CHANGE THIS & USE SECRETS MANAGER
        const string adminRoleName = Roles.Admin;      // Use constant if defined

        // --- Ensure Admin User Exists ---
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        bool adminCreated = false;
        if (adminUser == null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createUserResult.Succeeded)
            {
                logger.LogError("Error creating admin user '{AdminEmail}'. Errors: {Errors}", adminEmail, string.Join(", ", createUserResult.Errors.Select(e => e.Description)));
                return; // Cannot proceed if user creation failed
            }
            logger.LogInformation("Admin user '{AdminEmail}' created successfully.", adminEmail);
            adminCreated = true;
        }
        else
        {
            logger.LogInformation("Admin user '{AdminEmail}' already exists.", adminEmail);
        }

        // --- Ensure Admin Role Exists ---
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            logger.LogError("'{AdminRoleName}' role does not exist. Cannot assign role or permissions.", adminRoleName);
            return; // Cannot proceed if role doesn't exist
        }

        // --- Assign User to Admin Role (if newly created or not already in role) ---
        if (adminCreated || !await userManager.IsInRoleAsync(adminUser, adminRoleName))
        {
            var addToRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (addToRoleResult.Succeeded) { logger.LogInformation("Assigned '{AdminRoleName}' role to user '{AdminEmail}'.", adminRoleName, adminEmail); }
            else { logger.LogError("Error assigning '{AdminRoleName}' role to '{AdminEmail}'. Errors: {Errors}", adminRoleName, adminEmail, string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))); }
        }

        // --- Seed Permissions/Claims for Admin Role ---
        var adminRole = await roleManager.FindByNameAsync(adminRoleName);
        if (adminRole == null)
        {
            logger.LogError("'{AdminRoleName}' role not found after attempting to seed.", adminRoleName);
            return; // Should not happen if RoleExistsAsync passed, but defensive check
        }

        // Define all permissions the Admin role should have
        var adminPermissions = new List<string>
        {
            Permissions.MenuItems.View,
            Permissions.MenuItems.Create,
            Permissions.MenuItems.Edit,
            Permissions.MenuItems.Delete,
            Permissions.Orders.ViewAll,
            Permissions.Orders.EditStatus,
            Permissions.Users.View,
            Permissions.Users.Edit,
            Permissions.Users.ManageRoles,
            Permissions.Reports.ViewSales,
            Permissions.Reports.ViewUsage
            // Add all other permissions needed for Admin role
        };

        // Get existing claims for the role
        var existingClaims = await roleManager.GetClaimsAsync(adminRole);
        var existingPermissionClaims = existingClaims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();

        // Add claims that are missing
        foreach (var permission in adminPermissions)
        {
            if (!existingPermissionClaims.Contains(permission))
            {
                var claim = new Claim("permission", permission); // Use consistent claim type "permission"
                var addClaimResult = await roleManager.AddClaimAsync(adminRole, claim);
                if (addClaimResult.Succeeded)
                {
                    logger.LogInformation("Added permission claim '{Permission}' to role '{AdminRoleName}'.", permission, adminRoleName);
                }
                else
                {
                    logger.LogError("Error adding permission claim '{Permission}' to role '{AdminRoleName}'. Errors: {Errors}", permission, adminRoleName, string.Join(", ", addClaimResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    // Optional: Define Role Constants if not already done elsewhere
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
    }
}