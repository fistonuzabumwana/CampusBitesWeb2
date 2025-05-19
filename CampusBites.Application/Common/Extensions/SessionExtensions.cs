// src/CampusBites.Application/Common/Extensions/SessionExtensions.cs
using Microsoft.AspNetCore.Http; // Uses interfaces from Abstractions package
using System.Text.Json;

// Change namespace to match new location
namespace CampusBites.Application.Common.Extensions;

public static class SessionExtensions
{
    public static void Set<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }

    public static T? Get<T>(this ISession session, string key)
    {
        var value = session.GetString(key);
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
}