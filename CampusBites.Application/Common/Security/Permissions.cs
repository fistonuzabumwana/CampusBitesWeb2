// src/CampusBites.Application/Common/Security/Permissions.cs
using System.Reflection;

namespace CampusBites.Application.Common.Security;

public static class Permissions
{
    // Helper method to generate all permission constants - useful for seeding
    public static List<string> GeneratePermissionsForModule(string module)
    {
        return new List<string>()
        {
            $"Permissions.{module}.View",
            $"Permissions.{module}.Create",
            $"Permissions.{module}.Edit",
            $"Permissions.{module}.Delete",
        };
    }

    // Group permissions by module/feature using nested static classes
    public static class MenuItems
    {
        public const string View = "Permissions.MenuItems.View";
        public const string Create = "Permissions.MenuItems.Create";
        public const string Edit = "Permissions.MenuItems.Edit";
        public const string Delete = "Permissions.MenuItems.Delete";
    }

    public static class Orders
    {
        public const string ViewAll = "Permissions.Orders.ViewAll"; // Admin view
        public const string ViewOwn = "Permissions.Orders.ViewOwn";   // User view their own
        public const string EditStatus = "Permissions.Orders.EditStatus"; // Admin update status
        // Add other order permissions as needed
    }

    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string ManageRoles = "Permissions.Users.ManageRoles";
        public const string Edit = "Permissions.Users.Edit"; // e.g., Lockout, confirm email etc.
        public const string Delete = "Permissions.Users.Delete"; // e.g., Lockout, confirm email etc.

        // Add Delete permission if needed
    }

    public static class Reports
    {
        public const string ViewSales = "Permissions.Reports.ViewSales";
        public const string ViewUsage = "Permissions.Reports.ViewUsage";
        // Add other report permissions
    }

    // Add other modules like Dashboard, Settings etc. as needed
    // public static class Dashboard
    // {
    //     public const string ViewAdmin = "Permissions.Dashboard.ViewAdmin";
    //     public const string ViewUser = "Permissions.Dashboard.ViewUser";
    // }

    // --- NEW HELPER METHOD ---
    /// <summary>
    /// Gets a list of all permission constants defined in the nested static classes.
    /// </summary>
    /// <returns>A list of permission strings.</returns>
    public static List<string> GetAllPermissions()
    {
        var allPermissions = new List<string>();
        // Get all nested public static classes (like MenuItems, Orders, Users etc.)
        var nestedTypes = typeof(Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            // Get all public static literal fields (our const strings) from the nested class
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                             .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string));

            foreach (var field in fields)
            {
                // Get the constant string value
                var value = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    allPermissions.Add(value);
                }
            }
        }
        return allPermissions;
    }
    // --- END HELPER METHOD ---

    // Note: The GeneratePermissionsForModule helper might be less useful now
    // public static List<string> GeneratePermissionsForModule(string module) { ... }

}