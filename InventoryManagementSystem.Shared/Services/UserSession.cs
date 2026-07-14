using System;
using InventoryManagementSystem.Domain;

namespace InventoryManagementSystem.Services
{
    public static class UserSession
    {
        public static User? CurrentUser { get; private set; }

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool IsAdmin => CurrentUser?.Role == "Admin";

        public static bool HasPermission(string permission) =>
            UserAccessService.HasPermission(CurrentUser, permission);
    }
}
