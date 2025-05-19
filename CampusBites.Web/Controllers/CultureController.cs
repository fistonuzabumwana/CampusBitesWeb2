// src/CampusBites.Web/Controllers/CultureController.cs
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using Microsoft.Extensions.Options; // Required for IOptions
using System.Linq; // Required for Any()

namespace CampusBites.Web.Controllers;

// No [ApiController] attribute needed if returning redirects/views primarily
// Route can be simplified if desired
[Route("[controller]/[action]")]
public class CultureController : Controller // Inherit from Controller for access to Response, Redirect etc.
{
    private readonly RequestLocalizationOptions _locOptions;

    // Inject configured options
    public CultureController(IOptions<RequestLocalizationOptions> locOptions, ILogger<CultureController> @object)
    {
        _locOptions = locOptions.Value ?? new RequestLocalizationOptions(); // Get options value
    }


    [HttpPost] // This action handles the POST from the language selector form
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        // Validate the received culture against supported cultures
        if (culture != null && _locOptions.SupportedUICultures != null &&
            _locOptions.SupportedUICultures.Any(c => c.Name.Equals(culture, StringComparison.OrdinalIgnoreCase)))
        {
            // Set the cookie that the CookieRequestCultureProvider reads
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, Path = "/" } // Make it persistent
            );
        }
        else
        {
            // Optional: Log invalid culture attempt or set TempData error
        }

        // Redirect back to the original URL
        return LocalRedirect(returnUrl ?? "/");
    }
}