// Areas/Identity/Pages/Account/Manage/EnableAuthenticator.cshtml.cs
// Requires NuGet package QRCoder installed in Web project

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using QRCoder;
using CampusBites.Infrastructure.Identity; // Required for QR Code generation

namespace CampusBites.Web.Areas.Identity.Pages.Account.Manage
{
    public class EnableAuthenticatorModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EnableAuthenticatorModel> _logger;
        private readonly UrlEncoder _urlEncoder; // Required for encoding parts of the URI

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        public EnableAuthenticatorModel(
            UserManager<ApplicationUser> userManager,
            ILogger<EnableAuthenticatorModel> logger,
            UrlEncoder urlEncoder) // Inject UrlEncoder
        {
            _userManager = userManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        /// <summary>
        /// Shared key formatted for display.
        /// </summary>
        public string SharedKey { get; set; } = string.Empty;

        /// <summary>
        /// The URI for the QR code. Use this with JavaScript QR Libs.
        /// </summary>
        public string AuthenticatorUri { get; set; } = string.Empty;

        /// <summary>
        /// Data URI for the QR code image (Base64 PNG). Use this for img src.
        /// </summary>
        public string QrCodeDataUri { get; set; } = string.Empty; // <-- ADDED THIS PROPERTY

        /// <summary>
        /// Recovery codes generated for the user.
        /// </summary>
        [TempData]
        public string[]? RecoveryCodes { get; set; }

        /// <summary>
        /// Message shown to the user.
        /// </summary>
        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string Code { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadSharedKeyAndQrCodeUriAsync(user);
            GenerateQrCodeDataUri(AuthenticatorUri); // <-- Generate QR code data

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
                await LoadSharedKeyAndQrCodeUriAsync(user);
                GenerateQrCodeDataUri(AuthenticatorUri); // <-- Regenerate QR code data on error
                return Page();
            }

            // Strip spaces and hyphens
            var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Input.Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user);
                GenerateQrCodeDataUri(AuthenticatorUri); // <-- Regenerate QR code data on error
                return Page();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var userId = await _userManager.GetUserIdAsync(user);
            _logger.LogInformation("User with ID '{UserId}' has enabled 2FA with an authenticator app.", userId);

            StatusMessage = "Your authenticator app has been verified.";

            if (await _userManager.CountRecoveryCodesAsync(user) == 0)
            {
                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                RecoveryCodes = recoveryCodes?.ToArray();
                return RedirectToPage("./GenerateRecoveryCodes");
            }
            else
            {
                return RedirectToPage("./TwoFactorAuthentication");
            }
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
                // Handle case where key is *still* null/empty after reset if necessary
            }

            SharedKey = FormatKey(unformattedKey ?? string.Empty);

            var email = await _userManager.GetEmailAsync(user);
            // Use app name from configuration or hardcode temporarily
            string issuer = _urlEncoder.Encode("CampusBites.Web"); // Name displayed in authenticator app
            string account = _urlEncoder.Encode(email ?? user.UserName ?? user.Id); // Use email, fallback to username/ID

            AuthenticatorUri = string.Format(
                AuthenticatorUriFormat,
                issuer,
                account,
                unformattedKey);
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.AsSpan(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        // --- ADDED METHOD TO GENERATE QR CODE IMAGE DATA URI ---
        private void GenerateQrCodeDataUri(string authenticatorUri)
        {
            if (string.IsNullOrEmpty(authenticatorUri))
            {
                QrCodeDataUri = string.Empty;
                return;
            }

            try
            {
                using (var qrGenerator = new QRCodeGenerator())
                using (var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q))
                // Use PngByteQRCode for better compatibility & smaller size
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeImageBytes = qrCode.GetGraphic(20); // Adjust module size (pixels per module)
                    QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(qrCodeImageBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate QR Code data URI.");
                QrCodeDataUri = string.Empty; // Ensure it's empty on failure
            }
        }
        // --- END ADDED METHOD ---
    }
}
