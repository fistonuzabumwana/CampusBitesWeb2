// src/CampusBites.Application/Common/Interfaces/IFileStorageService.cs
using System.IO;
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IFileStorageService
{
    /// <summary>
    /// Saves a file from a stream.
    /// </summary>
    /// <param name="stream">The file stream.</param>
    /// <param name="fileName">The original file name (used for extension).</param>
    /// <param name="containerName">Optional container/folder name (e.g., "menu-items").</param>
    /// <returns>The relative web path where the file was saved (e.g., /uploads/menu-items/guid.jpg).</returns>
    Task<string> SaveFileAsync(Stream stream, string fileName, string containerName = "default");

    /// <summary>
    /// Deletes a file using its relative web path.
    /// </summary>
    /// <param name="filePath">The relative web path of the file to delete (e.g., /uploads/menu-items/guid.jpg).</param>
    Task DeleteFileAsync(string? filePath);

}