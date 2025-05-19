// src/CampusBites.Infrastructure/Services/LocalStorageService.cs
using CampusBites.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;


namespace CampusBites.Infrastructure.Services;

public class LocalStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<LocalStorageService> _logger;
    // Base path within wwwroot where files will be stored
    private readonly string _baseStoragePath = "uploads";
    private readonly IConfiguration _configuration; // Inject Configuration
    private readonly string _configKeyName = "FileEncryptionSettings:AESKey"; // Config key for AES Key


    public LocalStorageService(
        IWebHostEnvironment webHostEnvironment,
        ILogger<LocalStorageService> logger,
        IConfiguration configuration) // Add IConfiguration parameter
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
        _configuration = configuration; // Assign injected configuration
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, string containerName = "default")
    {
        // 1. Get and Validate Encryption Key
        string base64Key = _configuration[_configKeyName]
                           ?? throw new InvalidOperationException($"'{_configKeyName}' not found in configuration (check User Secrets or other providers).");
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(base64Key);
            if (keyBytes.Length != 32) // AES-256 requires a 32-byte key
            {
                throw new InvalidOperationException($"Configured AESKey ('{_configKeyName}') is not a valid length for AES-256 (must be 32 bytes).");
            }
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"Configured AESKey ('{_configKeyName}') is not a valid Base64 string.", ex);
        }

        // --- Start Encryption Process ---

        try
        {
            // Ensure container name is safe
            containerName = Path.GetFileName(containerName); // Basic sanitization
            string wwwRootPath = _webHostEnvironment.WebRootPath ?? throw new InvalidOperationException("wwwroot folder not found.");
            string containerPath = Path.Combine(wwwRootPath, _baseStoragePath, containerName);
            // Ensure the directory exists
            if (!Directory.Exists(containerPath))
            {
                Directory.CreateDirectory(containerPath);
            }
            string fileExtension = Path.GetExtension(fileName);
            string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            string fullPath = Path.Combine(containerPath, uniqueFileName);

            // Create AES instance (using statement ensures disposal)
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV(); // Generate a unique IV per file is crucial
            byte[] iv = aes.IV;

            // Save the file stream
            // Use FileStream with FileMode.Create to create or overwrite
            // Open the output file stream
            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);

            // Write the IV to the beginning of the file (16 bytes for AES)
            await fileStream.WriteAsync(iv, 0, iv.Length);

            // Create the encryptor and CryptoStream
            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            await using var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write);

            // Copy the input stream data through the crypto stream (encrypts on the fly)
            await stream.CopyToAsync(cryptoStream);
            // Return the relative web path (important for src attribute in img tags)
            // Combine "/" with base path, container name, and unique file name
            string relativePath = $"/{_baseStoragePath}/{containerName}/{uniqueFileName}".Replace('\\', '/');
            _logger.LogInformation("File encrypted and saved successfully to {FullPath}. Relative path: {RelativePath}", fullPath, relativePath);
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting and saving file {FileName} to container {ContainerName}", fileName, containerName);
            throw; ; // Re-throw the exception to be handled higher up
        }
    }

    public Task DeleteFileAsync(string? relativeFilePath)
    {
        if (string.IsNullOrEmpty(relativeFilePath))
        {
            _logger.LogWarning("Attempted to delete file with null or empty path.");
            return Task.CompletedTask; // Nothing to delete
        }

        try
        {
            string wwwRootPath = _webHostEnvironment.WebRootPath ?? throw new InvalidOperationException("wwwroot folder not found for deletion.");
            string physicalPath = Path.Combine(wwwRootPath, relativeFilePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
                _logger.LogInformation("Encrypted file deleted successfully: {PhysicalPath}", physicalPath);
            }
            else
            {
                _logger.LogWarning("Attempted to delete file that does not exist: {PhysicalPath}", physicalPath);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {RelativeFilePath}", relativeFilePath);
        }
        return Task.CompletedTask;
    }
}