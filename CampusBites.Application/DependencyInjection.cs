// src/CampusBites.Application/DependencyInjection.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Services;
using Microsoft.Extensions.DependencyInjection;
// using System.Reflection; // Needed if using AutoMapper AddMaps

namespace CampusBites.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Application Services
        services.AddScoped<IMenuItemService, MenuItemService>();

        // Add other services later (e.g., CartService, OrderService)
         services.AddScoped<ICartService, CartService>();

        // --- ADD ORDER SERVICE REGISTRATION ---
        services.AddScoped<IOrderService, OrderService>();
        // --- END ADD ---

        // If using AutoMapper, register it here:
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // --- ADD AUDIT SERVICE REGISTRATION ---
        services.AddScoped<IAuditService, AuditService>(); // Scoped might be okay, or Transient
        // --- END ADD ---
        // Add MediatR, FluentValidation etc. here if needed

        return services;
    }
}