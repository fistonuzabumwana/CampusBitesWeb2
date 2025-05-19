// src/CampusBites.Infrastructure/Security/AesGcmEncryptionHelper.cs
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CampusBites.Infrastructure.Security;

public static class AesGcmEncryptionHelper
{
    private const int NonceSize = 12; // Standard for AES-GCM
    private const int TagSize = 16;   // Standard for AES-GCM

    public static string? Encrypt(string? plainText, byte[] key)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText; // Don't encrypt null/empty
        if (key == null || key.Length != 32) throw new ArgumentException("Invalid key size for AES-256.", nameof(key));

        // Generate a random nonce (must be unique per encryption with the same key)
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSize];

        using (var aesGcm = new AesGcm(key, TagSize)) // Use key, specify tag size
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Combine nonce, tag, and ciphertext for storage
        byte[] encryptedData = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, encryptedData, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, encryptedData, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, encryptedData, NonceSize + TagSize, cipherBytes.Length);

        // Return as Base64 string
        return Convert.ToBase64String(encryptedData);
    }

    public static string? Decrypt(string? encryptedText, byte[] key)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText; // Nothing to decrypt
        if (key == null || key.Length != 32) throw new ArgumentException("Invalid key size for AES-256.", nameof(key));

        byte[] encryptedData;
        try
        {
            encryptedData = Convert.FromBase64String(encryptedText);
        }
        catch (FormatException)
        {
            // Handle case where data in DB isn't valid Base64 (e.g., old plaintext data)
            // Maybe return original string, log error, or throw? Returning original for now.
            Console.Error.WriteLine($"Warning: Could not Base64 decode value '{encryptedText}' for decryption.");
            return encryptedText;
        }


        if (encryptedData.Length < NonceSize + TagSize)
        {
            // Handle case where data is too short (e.g., old plaintext data)
            Console.Error.WriteLine($"Warning: Encrypted data '{encryptedText}' is too short for AES-GCM decryption.");
            return encryptedText; // Return original data if it doesn't look like valid encrypted data
        }

        // Extract nonce, tag, and ciphertext
        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        byte[] cipherBytes = new byte[encryptedData.Length - NonceSize - TagSize];

        Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

        byte[] plainBytes = new byte[cipherBytes.Length];

        try
        {
            using (var aesGcm = new AesGcm(key, TagSize))
            {
                aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException ex)
        {
            // Decryption failed - likely wrong key or tampered data
            Console.Error.WriteLine($"AES-GCM decryption failed: {ex.Message}. Returning original encrypted value.");
            // Handle failure appropriately - return original encrypted text, null, or throw?
            // Returning original encrypted text might expose it, but prevents app crash.
            // Consider logging this serious failure.
            return encryptedText;
        }
    }
}