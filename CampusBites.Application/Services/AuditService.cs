// src/CampusBites.Application/Services/AuditService.cs (or Infrastructure/Services)
using CampusBites.Application.Common.Interfaces;
using CampusBites.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CampusBites.Application.Services; // Adjust if placed elsewhere

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }


    public async Task LogAsync(string action, string? userId = null, string? entityType = null, string? entityId = null, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            _logger.LogWarning("Attempted to log audit event with empty action.");
            return; // Or throw? Don't log empty actions.
        }

        var auditLog = new AuditLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details?.Length > 2000 ? details.Substring(0, 2000) : details // Truncate details if too long
        };

        try
        {
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync(CancellationToken.None); // Save immediately
            _logger.LogInformation("Audit logged: User={UserId}, Action={Action}, Entity={EntityType}:{EntityId}", userId ?? "System", action, entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save audit log: User={UserId}, Action={Action}", userId ?? "System", action);
            // Decide if failure to log should throw an exception or just be logged
        }
    }
}