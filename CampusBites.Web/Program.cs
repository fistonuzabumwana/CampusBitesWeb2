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

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Add/Configure Localization Services ---
        // 1. Add Localization service, specifying Resource path
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

        // 2. Configure Razor Pages/MVC to support View & DataAnnotations localization
        builder.Services.AddRazorPages()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization(options =>
            {
                // Optional: Register a shared resource type for DataAnnotations validation messages
                // Requires creating a dummy class e.g. Resources/SharedResource.cs
                // options.DataAnnotationLocalizerProvider = (type, factory) =>
                //     factory.Create(typeof(Resources.SharedResource));
            });

        // 3. Configure Request Localization Options
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            // Define supported cultures
            var supportedCultures = new[]
            {
        new CultureInfo("en-US"), // English (United States) - Default
        new CultureInfo("fr-RW"), // French (Rwanda)
        new CultureInfo("rw-RW")  // Kinyarwanda (Rwanda)
                                  // Add other languages as needed
            };

            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            // IMPORTANT: Add CookieRequestCultureProvider to read preference from cookie
            // It looks for a cookie named ".AspNetCore.Culture" by default
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
        });

        // --- End Localization Services ---
        builder.Services.AddControllersWithViews()
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create(typeof(CampusBites.Web.Resources.SharedResource));
            });




        // --- Service Configuration ---
        builder.Services.AddHttpContextAccessor(); // Needed for session access in services
        builder.Services.AddDistributedMemoryCache(); // Needed for default session storage
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Example timeout
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        //builder.Services.AddRazorPages();
        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        // --- End Service Configuration ---

        // --- REGISTER NOTIFICATION SERVICE ---
        builder.Services.AddTransient<IDashboardNotifier, SignalRDashboardNotifier>();
        // --- END REGISTER ---
        // In Program.cs or Startup.cs
        builder.Services.AddAutoMapper(typeof(MappingProfile));

        builder.Services.AddScoped<IMenuItemService, MenuItemService>();


        var app = builder.Build();

        // Replace the following line:
        // Replace the following line:  
        // var locOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();  




        // Apply localization
        // --- Add Request Localization Middleware ---
        // IMPORTANT: Add this early in the pipeline, after Routing but before components
        // that need the culture (like Auth, MVC/Razor Pages).
        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()?.Value;
        if (localizationOptions != null)
        {
            app.UseRequestLocalization(localizationOptions);
        }
        else
        {
            // Handle error - options not configured? Should not happen if configured above.
            app.UseRequestLocalization(); // Use defaults? Might cause issues.
            app.Logger.LogWarning("RequestLocalizationOptions not found, using defaults.");
        }
        // --- End Add Middleware ---


        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;


        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            // Log initialization attempt (optional)
            var logger = services.GetRequiredService<ILogger<Program>>(); // Get logger if needed
            logger.LogInformation("Attempting database initialization and seeding...");

            await ApplicationDbInitializer.InitializeDatabaseAsync(services);
        }

        // --- Middleware Pipeline Configuration ---
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            // app.UseDeveloperExceptionPage(); // Often useful too
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();



        // --- ADD SESSION MIDDLEWARE HERE ---
        app.UseSession();
        // --- END ADD ---

        app.UseAuthentication();
        app.UseAuthorization(); // Must come after UseAuthentication

        app.MapRazorPages();
        app.MapControllers();
        app.MapHub<DashboardHub>("/dashboardHub");


        // app.MapFallbackToFile("index.html"); // Usually for SPAs

        // --- End Middleware Pipeline Configuration ---

        app.Run();
    }
}