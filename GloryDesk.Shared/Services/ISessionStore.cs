using System;

namespace InventoryManagementSystem.Services
{
    public class UserSessionData
    {
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? AuthToken { get; set; }
        public string? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface ISessionStore
    {
        UserSessionData? GetSession();
        void SaveSession(UserSessionData session);
        void ClearSession();
    }

    public class NullSessionStore : ISessionStore
    {
        public UserSessionData? GetSession() => null;
        public void SaveSession(UserSessionData session) { }
        public void ClearSession() { }
    }
}
