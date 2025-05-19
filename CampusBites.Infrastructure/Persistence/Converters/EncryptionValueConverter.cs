// src/CampusBites.Infrastructure/Persistence/Converters/EncryptionValueConverter.cs
using CampusBites.Infrastructure.Security; // For AesGcmEncryptionHelper
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;

namespace CampusBites.Infrastructure.Persistence.Converters;

public class EncryptionValueConverter : ValueConverter<string, string>
{
    public EncryptionValueConverter(byte[] encryptionKey, ConverterMappingHints? mappingHints = null)
        : base(
            // Encrypt function (Model -> Provider)
            v => AesGcmEncryptionHelper.Encrypt(v, encryptionKey) ?? string.Empty, // Store empty if null/empty input
                                                                                   // Decrypt function (Provider -> Model)
            v => AesGcmEncryptionHelper.Decrypt(v, encryptionKey) ?? string.Empty, // Return empty if null/empty input
            mappingHints)
    {
    }
}