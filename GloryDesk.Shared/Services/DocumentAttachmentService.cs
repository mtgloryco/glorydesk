using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Domain;
using InventoryManagementSystem.Infrastructure;

namespace InventoryManagementSystem.Services
{
    public class DocumentAttachmentService
    {
        private readonly DatabaseService _databaseService;
        private readonly AuditService? _auditService;

        public DocumentAttachmentService(DatabaseService databaseService, AuditService? auditService = null)
        {
            _databaseService = databaseService;
            _auditService = auditService;
        }

        public async Task<List<DocumentAttachment>> GetAttachmentsAsync(string entityType, int entityId)
        {
            return await _databaseService.Connection.Table<DocumentAttachment>()
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
        }

        public async Task<DocumentAttachment> AddAttachmentAsync(
            string entityType,
            int entityId,
            string sourceFilePath,
            string username)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                throw new InvalidOperationException("Source file was not found.");
            }

            var fileName = Path.GetFileName(sourceFilePath);
            var folder = Path.Combine(
                AppPaths.GetLocalAppDataFolder(),
                "attachments",
                Sanitize(entityType),
                entityId.ToString());
            Directory.CreateDirectory(folder);

            var storedName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{fileName}";
            var destPath = Path.Combine(folder, storedName);
            File.Copy(sourceFilePath, destPath, overwrite: false);

            var info = new FileInfo(destPath);
            var attachment = new DocumentAttachment
            {
                EntityType = entityType,
                EntityId = entityId,
                FileName = fileName,
                StoredPath = destPath,
                ContentType = GuessContentType(fileName),
                SizeBytes = info.Length,
                UploadedByUsername = username,
                UploadedAt = DateTime.UtcNow
            };

            await _databaseService.Connection.InsertAsync(attachment);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "AttachmentAdded", entityType, entityId, attachment);
            }

            return attachment;
        }

        public async Task DeleteAttachmentAsync(int attachmentId, string username)
        {
            var attachment = await _databaseService.Connection.FindAsync<DocumentAttachment>(attachmentId)
                ?? throw new InvalidOperationException("Attachment not found.");

            try
            {
                if (File.Exists(attachment.StoredPath))
                {
                    File.Delete(attachment.StoredPath);
                }
            }
            catch
            {
                // Best effort file cleanup
            }

            await _databaseService.Connection.DeleteAsync(attachment);

            if (_auditService != null)
            {
                await _auditService.LogActionAsync(username, "AttachmentDeleted", attachment.EntityType, attachment.EntityId, attachment);
            }
        }

        public void OpenAttachment(DocumentAttachment attachment)
        {
            if (attachment == null || !File.Exists(attachment.StoredPath))
            {
                throw new InvalidOperationException("Attachment file is missing.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = attachment.StoredPath,
                UseShellExecute = true
            });
        }

        private static string Sanitize(string value) =>
            string.Concat((value ?? "misc").Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'));

        private static string GuessContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
    }
}
