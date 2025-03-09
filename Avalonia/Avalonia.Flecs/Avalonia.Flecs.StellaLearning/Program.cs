using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopNotifications;
using DesktopNotifications.Avalonia;

namespace Avalonia.Flecs.StellaLearning;


/*
We implement stella learning as a single app, you cannot run more than one instance of the same app.
Because we want to run stella learning in the background. And instead of opening a new instance 
of the app we check if it already runs and show its window instead. This works without any problems on windows.
TODO: On Linux on the otherhand it seems to not work. I know that under the hood linux/macos would use
websockets as a fallback when named pipes are used, but I wouldnt see why this is a problem, maybe because of the naming?
*/
static class Program
{
    private const string AppMutexId = "StellaLearning-F86E70DA-7DF5-4B0A-9511-7C8151EFF94B";
    private const string PipeName = "StellaLearningPipe";
    private static Mutex? _mutex;
    private static Task? _pipeServerTask;
    public static INotificationManager NotificationManager = null!;
    public static bool StartHidden { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Parse command line arguments
        ParseCommandLineArgs(args);
        // Try to create a new mutex with our unique ID
        _mutex = new Mutex(true, AppMutexId, out bool isNewInstance);

        if (!isNewInstance)
        {
            // If we're not the first instance, signal the existing instance to show its window
            // Only show the window if not in hidden mode
            if (!StartHidden)
            {
                // If we're not the first instance, signal the existing instance to show its window
                SignalExistingInstance();
            }
            return; // Exit this instance
        }

        // We are the first instance, so start the pipe server to listen for signals
        _pipeServerTask = Task.Run(StartPipeServer);

        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
        // Clean up when application exits
        _mutex!.ReleaseMutex();
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .SetupDesktopNotifications(out NotificationManager!)
                .WithInterFont()
                .LogToTrace();
    }

    //TODO: The hidden feature flag does not work, 
    // Because we have to differently configure the app
    // as .StartWithClassicDesktopLifetime(args) automatically shows the window
    // we would have to say something like SetupWithoutStarting. 
    // But currently I dont know how exaclty this should look like
    private static void ParseCommandLineArgs(string[] args)
    {
        // Check if --hidden flag is present
        StartHidden = args.Any(arg =>
            arg.Equals("--hidden", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase));
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipeClient.Connect(1000); // Wait up to 1 second

            using var writer = new StreamWriter(pipeClient);
            writer.WriteLine("show");
            writer.Flush();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to signal existing instance: {ex.Message}");
        }
    }

    private static async Task StartPipeServer()
    {
        while (true)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In);
                await pipeServer.WaitForConnectionAsync();

                using var reader = new StreamReader(pipeServer);
                string? message = await reader.ReadLineAsync();

                if (message == "show")
                {
                    // Signal the UI thread to show/activate the main window
                    await Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (App.Current is App app)
                        {
                            app.ShowMainWindow();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Pipe server error: {ex.Message}");
                await Task.Delay(1000); // Wait before retrying
            }
        }
    }
}
