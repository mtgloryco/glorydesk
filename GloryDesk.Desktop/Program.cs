using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                HandleFatalException(ex);
            }
        };

        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            HandleFatalException(eventArgs.Exception);
            eventArgs.SetObserved();
        };

        try
        {
            Velopack.VelopackApp.Build().Run();

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            HandleFatalException(ex);
        }
    }

    private static void HandleFatalException(Exception ex)
    {
        try
        {
            var logFolder = Path.Combine(AppPaths.GetLocalAppDataFolder(), "logs");
            Directory.CreateDirectory(logFolder);
            var logFile = Path.Combine(logFolder, "crash.log");
            var crashText = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] FATAL EXCEPTION:\n{ex}\n\n";
            File.AppendAllText(logFile, crashText);
            Console.Error.WriteLine(crashText);

            ShowNativeMessageBox(
                "Glory Desk - Startup Error",
                $"An unexpected error caused Glory Desk to stop:\n\n{ex.Message}\n\nDetails have been logged to:\n{logFile}");
        }
        catch
        {
            Console.Error.WriteLine($"Fatal crash: {ex}");
        }
    }

    private static void ShowNativeMessageBox(string title, string message)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                MessageBoxW(IntPtr.Zero, message, title, 0x00000010 /* MB_ICONERROR */);
                return;
            }
            catch { }
        }
        else if (OperatingSystem.IsLinux())
        {
            try
            {
                if (File.Exists("/usr/bin/kdialog"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "kdialog",
                        Arguments = $"--title \"{EscapeArg(title)}\" --error \"{EscapeArg(message)}\"",
                        UseShellExecute = false
                    })?.WaitForExit(3000);
                    return;
                }
                if (File.Exists("/usr/bin/zenity"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "zenity",
                        Arguments = $"--error --title=\"{EscapeArg(title)}\" --text=\"{EscapeArg(message)}\"",
                        UseShellExecute = false
                    })?.WaitForExit(3000);
                    return;
                }
                if (File.Exists("/usr/bin/notify-send"))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "notify-send",
                        Arguments = $"-u critical \"{EscapeArg(title)}\" \"{EscapeArg(message)}\"",
                        UseShellExecute = false
                    })?.WaitForExit(3000);
                    return;
                }
            }
            catch { }
        }
    }

    private static string EscapeArg(string arg) => arg.Replace("\"", "\\\"").Replace("\n", " ");

    [DllImport("user32.dll", EntryPoint = "MessageBoxW", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new X11PlatformOptions 
            {
                EnableMultiTouch = true,
                UseDBusMenu = true,
                RenderingMode = new[] { X11RenderingMode.Glx, X11RenderingMode.Software }
            });
}
