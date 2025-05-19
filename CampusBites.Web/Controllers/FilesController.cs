// src/CampusBites.Web/Controllers/FilesController.cs
using CampusBites.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization; // To secure the endpoint
using Microsoft.AspNetCore.Hosting;      // To find wwwroot
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles; // For Content Type Provider
using Microsoft.Extensions.Configuration; // To get encryption key
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography; // For AES, CryptoStream
using System.Threading.Tasks;

namespace CampusBites.Web.Controllers;

[Route("api/[controller]")] // Route: api/files
[ApiController]
[Authorize] // Secure the controller - users must be logged in to view images
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FilesController> _logger;
    private readonly string _baseStoragePath = "uploads"; // Matches LocalStorageService
    private readonly string _configKeyName = "FileEncryptionSettings:AESKey"; // Matches LocalStorageService

    public FilesController(
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration,
        ILogger<FilesController> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _logger = logger;
    }



    // GET: api/files/menu-item-image/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.jpg
    [HttpGet("menu-item-image/{fileName}")]
    [AllowAnonymous] // OR use specific policy if needed, e.g., [Authorize(Policy=Permissions.MenuItems.View)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetMenuItemImage(string fileName)
    {
        // 1. Validate fileName (basic)
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            _logger.LogWarning("Invalid file name requested: {FileName}", fileName);
            return BadRequest("Invalid file name.");
        }

        // 2. Get Encryption Key
        string base64Key = _configuration[_configKeyName] ?? string.Empty;
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(base64Key);
            if (keyBytes.Length != 32) throw new InvalidOperationException("AESKey length invalid.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid AESKey configuration ('{ConfigKey}'). Cannot decrypt file.", _configKeyName);
            return StatusCode(StatusCodes.Status500InternalServerError, "Server configuration error.");
        }


        // 3. Construct Full Path to Encrypted File
        string containerName = "menu-items"; // Matches where files were saved
        string wwwRootPath = _webHostEnvironment.WebRootPath ?? string.Empty;
        string encryptedFilePath = Path.Combine(wwwRootPath, _baseStoragePath, containerName, fileName);

        if (!System.IO.File.Exists(encryptedFilePath))
        {
            _logger.LogWarning("Encrypted file not found: {FilePath}", encryptedFilePath);
            return NotFound("File not found.");
        }

        try
        {
            // 4. Open FileStream for reading the encrypted file
            // IMPORTANT: Do NOT use 'using' here yet, as FileStreamResult will manage disposal
            var encryptedFileStream = new FileStream(encryptedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // 5. Read the IV (first 16 bytes)
            byte[] iv = new byte[16]; // AES IV is always 16 bytes
            int bytesRead = encryptedFileStream.Read(iv, 0, iv.Length);
            if (bytesRead < 16)
            {
                encryptedFileStream.Dispose(); // Must dispose if not passing to FileStreamResult
                _logger.LogError("Could not read full IV from encrypted file: {FilePath}", encryptedFilePath);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error reading file.");
            }

            // 6. Setup AES for Decryption
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = iv;

            // 7. Create Decryptor and CryptoStream in Read mode
            // The CryptoStream will wrap the FileStream but start reading *after* the IV
            // IMPORTANT: Do NOT dispose the CryptoStream here if passing it to FileStreamResult
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var cryptoStream = new CryptoStream(encryptedFileStream, decryptor, CryptoStreamMode.Read);

            // 8. Determine Content Type (MIME Type)
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream"; // Default fallback
            }

            _logger.LogInformation("Serving decrypted file {FileName} with content type {ContentType}", fileName, contentType);

            // 9. Return FileStreamResult
            // This streams the decrypted content directly to the response.
            // It takes ownership of the CryptoStream (and the underlying FileStream) and disposes them correctly.
            return new FileStreamResult(cryptoStream, contentType);

        }
        catch (CryptographicException cryptoEx)
        {
            _logger.LogError(cryptoEx, "Decryption failed for file: {FilePath}", encryptedFilePath);
            return StatusCode(StatusCodes.Status500InternalServerError, "Cannot decrypt file."); // Don't give too many details
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving encrypted file: {FilePath}", encryptedFilePath);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the file.");
        }
    }


    [HttpGet("profile-image/{fileName}")]
    [Authorize] // Only authenticated users can access
    public IActionResult GetProfileImage(string fileName)
    {
        // Validate filename
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            _logger.LogWarning("Invalid profile image file name requested: {FileName}", fileName);
            return BadRequest("Invalid filename");
        }

        // Get AES key
        string base64Key = _configuration["FileEncryptionSettings:AESKey"] ?? string.Empty;
        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key);
            if (key.Length != 32)
                throw new InvalidOperationException("Invalid AES key length.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid AES key in configuration.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Encryption key error.");
        }

        // Construct full path to encrypted file
        var fullPath = Path.Combine(
            _webHostEnvironment.WebRootPath ?? string.Empty,
            _baseStoragePath,
            "profile-pictures",
            fileName);

        if (!System.IO.File.Exists(fullPath))
        {
            _logger.LogWarning("Profile image not found: {Path}", fullPath);
            return NotFound("Profile image not found.");
        }

        try
        {
            var encryptedFileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Read IV (first 16 bytes)
            byte[] iv = new byte[16];
            int bytesRead = encryptedFileStream.Read(iv, 0, iv.Length);
            if (bytesRead < 16)
            {
                encryptedFileStream.Dispose();
                _logger.LogError("IV read error for profile image: {Path}", fullPath);
                return StatusCode(500, "Error reading profile image.");
            }

            // Setup AES decryption
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var cryptoStream = new CryptoStream(encryptedFileStream, decryptor, CryptoStreamMode.Read);

            // Determine MIME type
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
                contentType = "application/octet-stream";

            _logger.LogInformation("Serving profile image: {FileName}", fileName);
            return new FileStreamResult(cryptoStream, contentType);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Decryption failed for profile image: {FileName}", fileName);
            return StatusCode(500, "Decryption failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error serving profile image: {FileName}", fileName);
            return StatusCode(500, "Error serving profile image.");
        }
    }

}