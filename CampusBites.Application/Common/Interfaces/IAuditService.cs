// src/CampusBites.Application/Common/Interfaces/IAuditService.cs
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Logs an audit event.
    /// </summary>
    /// <param name="action">Description of the action performed.</param>
    /// <param name="userId">ID of the user performing the action (null if system/anonymous).</param>
    /// <param name="entityType">Optional: Name of the entity type affected.</param>
    /// <param name="entityId">Optional: Primary key of the entity affected.</param>
    /// <param name="details">Optional: Additional details about the action or changes.</param>
    Task LogAsync(string action, string? userId = null, string? entityType = null, string? entityId = null, string? details = null);
}