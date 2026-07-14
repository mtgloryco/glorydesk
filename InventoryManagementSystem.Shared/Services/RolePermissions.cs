using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Services
{
    public static class RolePermissions
    {
        public const string AccessPOS = "AccessPOS";
        public const string ManageInventory = "ManageInventory";
        public const string ViewInventory = "ViewInventory";
        public const string ManageSales = "ManageSales";
        public const string ManagePurchasing = "ManagePurchasing";
        public const string ManageSuppliers = "ManageSuppliers";
        public const string ManageCustomers = "ManageCustomers";
        public const string ProcessReturns = "ProcessReturns";
        public const string ViewReports = "ViewReports";
        public const string ViewAudit = "ViewAudit";
        public const string ManageUsers = "ManageUsers";
        public const string ManageSettings = "ManageSettings";
        public const string ManageManufacturing = "ManageManufacturing";

        private static readonly HashSet<string> AllPermissions = new(StringComparer.OrdinalIgnoreCase)
        {
            AccessPOS, ManageInventory, ViewInventory, ManageSales, ManagePurchasing,
            ManageSuppliers, ManageCustomers, ProcessReturns, ViewReports, ViewAudit,
            ManageUsers, ManageSettings, ManageManufacturing
        };

        private static readonly Dictionary<string, HashSet<string>> RoleMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Admin"] = AllPermissions,
            ["Manager"] = new HashSet<string>(AllPermissions, StringComparer.OrdinalIgnoreCase) { },
            ["Accountant"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ViewInventory, ManageSales, ManagePurchasing, ViewReports, ViewAudit, ManageSettings
            },
            ["Cashier"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                AccessPOS, ViewInventory, ProcessReturns
            },
            ["Staff"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ManageInventory, ViewInventory, ManageSales, ManagePurchasing,
                ManageSuppliers, ManageCustomers, ProcessReturns, ViewReports, ManageManufacturing
            },
            ["Guest"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ViewInventory, ViewReports
            }
        };

        static RolePermissions()
        {
            RoleMap["Manager"].Remove(ManageUsers);
        }

        public static bool HasPermission(string? role, string permission)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return RoleMap.TryGetValue(role, out var permissions)
                   && permissions.Contains(permission);
        }

        public static IReadOnlyList<string> GetAssignableRoles() =>
            new[] { "Admin", "Manager", "Accountant", "Cashier", "Staff" };
    }
}
