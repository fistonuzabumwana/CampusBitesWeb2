using CampusBites.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CampusBites.Infrastructure;
using CampusBites.Application;
using CampusBites.Infrastructure.Identity;
using System.Security.Cryptography;
using CampusBites.Web.Hubs;
using CampusBites.Application.Common.Interfaces;
using CampusBites.Web.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Razor;
using CampusBites.Application.Services;
using System.Reflection; // Ensure this is present if using Assembly.GetEntryAssembly() for AutoMapper

internal class Program
{
    private static async Task Main(string[] args)
    {
        // This line is crucial. WebApplication.CreateBuilder(args) automatically sets up:
        // 1. appsettings.json
        // 2. appsettings.{EnvironmentName}.json (e.g., appsettings.Development.json)
        // 3. User Secrets (if in Development environment)
        // 4. Environment Variables
        // 5. Command-line arguments
        var builder = WebApplication.CreateBuilder(args);

        // --- CUSTOM CONFIGURATION ADDITION (no clearing needed) ---
        // ONLY add appsettings.gcp.json when in the "Gcp" environment.
        // This file will then override any matching settings from appsettings.json or
        // appsettings.Production.json (if ASPNETCORE_ENVIRONMENT was "Production" instead of "Gcp").
        if (builder.Environment.IsEnvironment("Gcp")) // "Gcp" is your custom environment name
        {
            builder.Configuration.AddJsonFile("appsettings.gcp.json", optional: true, reloadOnChange: true);
        }
        // No need to explicitly add AddEnvironmentVariables() here, as CreateBuilder does it by default
        // and it's already the last source, so it always overrides everything loaded before it.
        // --- END CUSTOM CONFIGURATION ADDITION ---


        // --- Add/Configure Localization Services ---
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddRazorPages()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization(options =>
            {
                // ... (your DataAnnotationLocalizerProvider setup)
            });
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("fr-RW"),
                new CultureInfo("rw-RW")
            };
            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
        });
        builder.Services.AddControllersWithViews()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create(typeof(CampusBites.Web.Resources.SharedResource));
            });


        // --- Service Configuration ---
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration); // Passes IConfiguration
        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        // --- REGISTER NOTIFICATION SERVICE ---
        builder.Services.AddTransient<IDashboardNotifier, SignalRDashboardNotifier>();

        // For AutoMapper, ensure it finds profiles in your project assemblies
        // If MappingProfile is in the same assembly as Program.cs (CampusBites.Web), typeof(MappingProfile) is fine.
        // If it's in another project like CampusBites.Application, you might use Assembly.GetEntryAssembly()
        // or typeof(SomeTypeInApplicationAssembly).Assembly
        builder.Services.AddAutoMapper(typeof(MappingProfile));

        builder.Services.AddScoped<IMenuItemService, MenuItemService>();

        var app = builder.Build();

        // Apply localization
        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()?.Value;
        if (localizationOptions != null)
        {
            app.UseRequestLocalization(localizationOptions);
        }
        else
        {
            app.UseRequestLocalization();
            app.Logger.LogWarning("RequestLocalizationOptions not found, using defaults.");
        }

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        // Database Initialization
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Attempting database initialization and seeding...");
            await ApplicationDbInitializer.InitializeDatabaseAsync(services);
        }

        // --- Middleware Pipeline Configuration ---
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();
        app.MapHub<DashboardHub>("/dashboardHub");

        // --- Database Initialization with Error Handling --- (This block is a duplicate of the one above)
        // You only need one block for database initialization. The one directly after QuestPDF.Settings.License
        // is usually sufficient. Removing this duplicate to avoid confusion.
        // try
        // {
        //     using (var scope = app.Services.CreateScope())
        //     {
        //         var services = scope.ServiceProvider;
        //         var logger = services.GetRequiredService<ILogger<Program>>();
        //         logger.LogInformation("Attempting database initialization...");
        //         await ApplicationDbInitializer.InitializeDatabaseAsync(services);
        //         logger.LogInformation("Database initialization completed.");
        //     }
        // }
        // catch (Exception ex)
        // {
        //     app.Logger.LogError(ex, "An error occurred while initializing the database");
        // }

        // --- Run Configuration ---
        var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
        var url = $"http://0.0.0.0:{port}";

        app.Logger.LogInformation($"Starting application on {url}");
        app.Run(url);
    }
}