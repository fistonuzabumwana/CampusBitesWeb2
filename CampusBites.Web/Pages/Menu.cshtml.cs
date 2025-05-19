// src/CampusBites.Web/Pages/Menu.cshtml.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // Optional: for logging

namespace CampusBites.Web.Pages;

public class MenuModel : PageModel
{
    private readonly ILogger<MenuModel> _logger; // Optional

    public MenuModel(ILogger<MenuModel> logger) // Optional
    {
        _logger = logger;
    }

    // We don't need OnGetAsync to fetch data here because
    // we'll use JavaScript to call the API after the page loads.
    public void OnGet()
    {
        // Page initialization logic if needed
    }
}