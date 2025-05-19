// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CampusBites.Application.Common.Interfaces;
using CampusBites.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusBites.Web.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IFileStorageService _fileStorageService;

        public string ErrorMessage;

        // Define claim type constant
        private const string OrderStatusEmailPrefClaimType = "notify_pref_order_status_email";



        // --- ADD Theme Preference Property ---
        [BindProperty]
        [Display(Name = "Theme Preference")]
        public string SelectedTheme { get; set; } = "System"; // Default to System
                                                              // --- END ADD ---

        // --- ADD Notification Preference Property ---
        [BindProperty]
        [Display(Name = "Receive Order Status Updates via Email")]
        public bool NotifyOrderStatusEmail { get; set; }
        // --- END ADD ---

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IFileStorageService fileStorageService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _fileStorageService = fileStorageService;

        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePictureUrl { get; set; }


        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }



        [BindProperty]
        public IFormFile ProfilePicture { get; set; }


        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }


            [Display(Name = "First Name")]
            [MaxLength(100)]
            public string FirstName { get; set; }

            [Display(Name = "Last Name")]
            [MaxLength(100)]
            public string LastName { get; set; }



        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            FirstName = user.FirstName;
            LastName = user.LastName;
            ProfilePictureUrl = user.ProfilePictureUrl;


            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
               
            };

            // --- Load Theme Preference ---
            var claims = await _userManager.GetClaimsAsync(user);
            var themeClaim = claims.FirstOrDefault(c => c.Type == "ui_theme");
            SelectedTheme = themeClaim?.Value ?? "System"; // Load claim or default
                                                           // --- END Load ---

            // --- Load Notification Preference ---
            var notifyClaim = claims.FirstOrDefault(c => c.Type == OrderStatusEmailPrefClaimType);
            // Default to true if no preference is saved? Or false? Let's default to true.
            NotifyOrderStatusEmail = notifyClaim?.Value?.ToLower() == "true" || notifyClaim == null;
            // --- END Load ---
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            bool changesMade = false; // Track if any changes were made


            // Handle profile picture upload
            if (ProfilePicture != null && ProfilePicture.Length > 0)
            {
                try
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(ProfilePicture.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ProfilePicture", "Only JPG, PNG, or GIF images are allowed.");
                        await LoadAsync(user);
                        return Page();
                    }

                    if (ProfilePicture.Length > 5 * 1024 * 1024) // 5MB
                    {
                        ModelState.AddModelError("ProfilePicture", "Image size must be less than 5MB.");
                        await LoadAsync(user);
                        return Page();
                    }

                    using var stream = ProfilePicture.OpenReadStream();
                    user.ProfilePictureUrl = await _fileStorageService.SaveFileAsync(
                        stream,
                        $"{user.Id}-profile{fileExtension}",
                        "profile-pictures");

                    await _userManager.UpdateAsync(user);
                    await _signInManager.RefreshSignInAsync(user);
                    changesMade = true; // Make sure this is set
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ProfilePicture", "Error uploading image: " + ex.Message);
                    await LoadAsync(user);
                    return Page();
                }

            }



            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            if (Input.FirstName != user.FirstName)
            {
                user.FirstName = Input.FirstName;
            }

            if (Input.LastName != user.LastName)
            {
                user.LastName = Input.LastName;
            }



            await _userManager.UpdateAsync(user);

 
        

            // --- Save Theme Preference ---
            var currentClaims = await _userManager.GetClaimsAsync(user);
            var currentThemeClaim = currentClaims.FirstOrDefault(c => c.Type == "ui_theme");
            string newThemeValue = SelectedTheme ?? "System"; // Ensure not null


            // Remove existing theme claim if it exists
            if (currentThemeClaim != null)
            {
                // Only remove if the new value is different OR if setting back to System default
                if (currentThemeClaim.Value != newThemeValue || newThemeValue == "System")
                {
                    var remResult = await _userManager.RemoveClaimAsync(user, currentThemeClaim);
                    if (!remResult.Succeeded) { StatusMessage = "Error removing old theme preference."; /* Handle error */ }
                }
            }

            // Add new claim only if Light or Dark is chosen (don't store "System")
            // And only if it wasn't already the current value (that wasn't removed)
            if (newThemeValue != "System" && currentThemeClaim?.Value != newThemeValue)
            {
                var addResult = await _userManager.AddClaimAsync(user, new Claim("ui_theme", newThemeValue));
                if (!addResult.Succeeded) { StatusMessage = "Error saving new theme preference."; /* Handle error */ }
            }
            // --- End Save Theme ---

            // --- Save Notification Preference ---
            var currentNotifyClaim = currentClaims.FirstOrDefault(c => c.Type == OrderStatusEmailPrefClaimType);
            string currentNotifyValue = currentNotifyClaim?.Value?.ToLower() ?? "true"; // Default to true if null
            string newNotifyValue = NotifyOrderStatusEmail ? "true" : "false";

            if (currentNotifyValue != newNotifyValue)
            {
                // Remove existing claim first if it exists
                if (currentNotifyClaim != null)
                {
                    var remResult = await _userManager.RemoveClaimAsync(user, currentNotifyClaim);
                    if (!remResult.Succeeded) { /* Handle error */ ErrorMessage = "Error removing old notification preference."; }
                }
                // Add the new claim (Value is "true" or "false")
                var addResult = await _userManager.AddClaimAsync(user, new Claim(OrderStatusEmailPrefClaimType, newNotifyValue));
                if (!addResult.Succeeded) { /* Handle error */ ErrorMessage = "Error adding new notification preference."; }
                else { changesMade = true; }
            }
            // --- End Save Notification Preference ---
            // Refresh cookie ONLY if claims or security stamp changed
            // Refresh cookie ONLY if claims or security stamp changed
            if (changesMade)
            {
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Your profile preferences have been updated";
            }
            else if (string.IsNullOrEmpty(ErrorMessage)) // Don't overwrite error message
            {
                StatusMessage = "Your profile is unchanged."; // Or just don't set message
            }

            return RedirectToPage();
        }
    }
}
