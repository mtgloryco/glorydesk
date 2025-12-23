using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace InventoryManagementSystem.Services
{
    public class HardwareIdService
    {
        public string GetCompositeHardwareId()
        {
            var cpuId = GetCpuIdentifier();
            var diskId = GetDiskSerialNumber();
            var machineId = GetMachineIdentifier();
            var macAddress = GetFirstMacAddress();

            var rawId = $"{cpuId}|{diskId}|{machineId}|{macAddress}";
            var normalizedId = rawId.Replace(" ", "").ToUpperInvariant();

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedId));
                var hex = Convert.ToHexString(hashBytes);
                // Return truncated 24 chars for readability
                return hex.Substring(0, 24);
            }
        }

        private string GetCpuIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try { return File.ReadAllText("/proc/cpuinfo").Split('\n').FirstOrDefault(l => l.Contains("Serial"))?.Split(':')[1].Trim() ?? Environment.ProcessorCount.ToString(); }
                catch { return Environment.ProcessorCount.ToString(); }
            }
            return Environment.ProcessorCount.ToString();
        }

        private string GetWindowsDiskSerial()
        {
            if (!OperatingSystem.IsWindows()) return "NOT-WINDOWS";
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia WHERE Tag='Disk'"))
                {
                    foreach (System.Management.ManagementObject disk in searcher.Get())
                    {
                        var serial = disk["SerialNumber"]?.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(serial)) return serial;
                    }
                }
            }
            catch { }
            return "UNKNOWN-WINDOWS-DISK";
        }

        private string GetLinuxDiskId()
        {
            try
            {
                // Try to get serial from /dev/disk/by-id/ (requires no root usually for reading some links)
                var diskPath = "/dev/disk/by-id";
                if (Directory.Exists(diskPath))
                {
                    var disks = Directory.GetFiles(diskPath);
                    var firstDisk = disks.FirstOrDefault(d => !d.Contains("-part"));
                    if (firstDisk != null) return Path.GetFileName(firstDisk);
                }

                // Fallback to reading from /sys/block/
                var sysBlock = "/sys/block";
                if (Directory.Exists(sysBlock))
                {
                    var devices = Directory.GetDirectories(sysBlock);
                    foreach (var dev in devices)
                    {
                        var serialPath = Path.Combine(dev, "device/serial");
                        if (File.Exists(serialPath))
                        {
                            var serial = File.ReadAllText(serialPath).Trim();
                            if (!string.IsNullOrEmpty(serial)) return serial;
                        }
                    }
                }
            }
            catch { }
            return "LINUX-DISK-SERIAL-UNKNOWN";
        }
        private string GetMacDiskUuid()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ioreg",
                        Arguments = "-rd1 -c IOPlatformExpertDevice",
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var match = Regex.Match(output, "\"IOPlatformUUID\" = \"(.+?)\"");
                if (match.Success)
                    return match.Groups[1].Value;
            }
            catch { }

            return "MAC-DISK-UNKNOWN";
        }

        private string GetDiskSerialNumber()
        {
            if (OperatingSystem.IsWindows())
                return GetWindowsDiskSerial();

            if (OperatingSystem.IsLinux())
                return GetLinuxDiskId();

            if (OperatingSystem.IsMacOS())
                return GetMacDiskUuid();

            return "UNKNOWN-DISK";
        }


        private string GetMachineIdentifier()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try { return File.ReadAllText("/etc/machine-id").Trim(); }
                catch { try { return File.ReadAllText("/var/lib/dbus/machine-id").Trim(); } catch { } }
            }
            return Environment.MachineName;
        }

        private string GetFirstMacAddress()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .Select(nic => nic.GetPhysicalAddress().ToString())
                    .FirstOrDefault() ?? "000000000000";
            }
            catch { return "000000000000"; }
        }
    }
}
