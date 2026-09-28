using System;
using System.IO;
using System.Text.Json;

namespace InventoryManagementSystem.Services
{
    public class FileSessionStore : ISessionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        private readonly string _sessionFilePath;

        public FileSessionStore(string? sessionFilePath = null)
        {
            var folder = AppPaths.GetLocalAppDataFolder();
            _sessionFilePath = sessionFilePath ?? Path.Combine(folder, "session.json");
        }

        public UserSessionData? GetSession()
        {
            try
            {
                if (!File.Exists(_sessionFilePath)) return null;
                var json = File.ReadAllText(_sessionFilePath);
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonSerializer.Deserialize<UserSessionData>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public void SaveSession(UserSessionData session)
        {
            try
            {
                var folder = Path.GetDirectoryName(_sessionFilePath);
                if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var json = JsonSerializer.Serialize(session, JsonOptions);
                File.WriteAllText(_sessionFilePath, json);
            }
            catch { }
        }

        public void ClearSession()
        {
            try
            {
                if (File.Exists(_sessionFilePath))
                {
                    File.Delete(_sessionFilePath);
                }
            }
            catch { }
        }
    }
}
