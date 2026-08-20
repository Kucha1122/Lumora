using Avalonia;
using System;

namespace Lumora.Client.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Last-resort net: a tray app has no window to show a crash dialog in, so an
        // unhandled exception otherwise vanishes and the app just disappears from the
        // tray with no explanation. Log it so at least it's diagnosable.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LogFatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            LogFatal(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void LogFatal(Exception? ex, string source)
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lumora", "crash.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"[{DateTimeOffset.Now:O}] {source}: {ex}\n\n");
        }
        catch
        {
            // Logging the crash must never itself throw.
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
