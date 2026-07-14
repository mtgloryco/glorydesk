using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using InventoryManagementSystem.Domain;

namespace InventoryManagementSystem.Services
{
    public static class UserAccessService
    {
        public static IReadOnlyList<(string Key, string Label)> AllModules { get; } = new List<(string, string)>
        {
            (RolePermissions.AccessPOS, "Point of Sale (POS)"),
            (RolePermissions.ManageInventory, "Inventory / Products"),
            (RolePermissions.ManageCustomers, "Customers"),
            (RolePermissions.ManageSuppliers, "Suppliers"),
            (RolePermissions.ManagePurchasing, "Purchase Orders & RFQ"),
            (RolePermissions.ManageSales, "Sales Orders & Quotations"),
            (RolePermissions.ProcessReturns, "Returns & Refunds"),
            (RolePermissions.ViewReports, "Reports & Analytics"),
            (RolePermissions.ViewAudit, "Audit Trail"),
            (RolePermissions.ManageManufacturing, "Manufacturing"),
            (RolePermissions.ManageSettings, "Settings"),
            (RolePermissions.ManageUsers, "User Management")
        };

        public static bool HasPermission(User? user, string permission)
        {
            if (user == null || !user.IsActive)
            {
                return false;
            }

            if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var custom = DeserializePermissions(user.PermissionsJson);
            if (custom.Count > 0)
            {
                return custom.Contains(permission);
            }

            return RolePermissions.HasPermission(user.Role, permission);
        }

        public static HashSet<string> GetEffectivePermissions(User user)
        {
            if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return AllModules.Select(m => m.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var custom = DeserializePermissions(user.PermissionsJson);
            if (custom.Count > 0)
            {
                return custom;
            }

            return AllModules
                .Where(m => RolePermissions.HasPermission(user.Role, m.Key))
                .Select(m => m.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static HashSet<string> GetDefaultPermissionsForRole(string role) =>
            AllModules
                .Where(m => RolePermissions.HasPermission(role, m.Key))
                .Select(m => m.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public static string SerializePermissions(IEnumerable<string> permissions)
        {
            var list = permissions.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p).ToList();
            return list.Count == 0 ? string.Empty : JsonSerializer.Serialize(list);
        }

        public static HashSet<string> DeserializePermissions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                return list.ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
