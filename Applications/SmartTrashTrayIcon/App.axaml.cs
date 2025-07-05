using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace SmartTrashTrayIcon;

/// <summary>
/// TEST
/// </summary>
public class App : Application
{
    private TrayIcon? _trayIcon;
    private DispatcherTimer? _timer;

    // For emptying the trash on Windows
    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    // For getting trash size on Windows
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    /// <summary>
    /// Trash info for windows
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHQUERYRBINFO
    {
        /// <summary>
        /// Size
        /// </summary>
        public int cbSize;
        /// <summary>
        /// size
        /// </summary>
        public long i64Size;
        /// <summary>
        /// Item number
        /// </summary>
        public long i64NumItems;
    }

    enum RecycleFlags : uint
    {
        SHERB_NOCONFIRMATION = 0x00000001,
        SHERB_NOPROGRESSUI = 0x00000002,
        SHERB_NOSOUND = 0x00000004
    }

    /// <summary>
    /// s
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        InitializeTrayIcon();
    }

    /// <summary>
    /// TEST
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        base.OnFrameworkInitializationCompleted();
    }


    private void InitializeTrayIcon()
    {
        try
        {
            // Load icon from resources
            var iconStream = AssetLoader.Open(
                new Uri("avares://SmartTrashTrayIcon/Assets/trash-icon.ico")
            );

            // Create the tray icon
            _trayIcon = new TrayIcon
            {
                Icon = new WindowIcon(iconStream),
                ToolTipText = "Trash Manager",
                IsVisible = true,
                Menu = new NativeMenu
                {
                    Items =
                    {
                        new NativeMenuItem("Open") { Command = new RelayCommand(OpenTrashBin) },
                        new NativeMenuItem("Clear") { Command = new RelayCommand(ClearTrashBin) },
                        new NativeMenuItemSeparator(),
                        new NativeMenuItem("Exit")
                        {
                            Command = new RelayCommand(ShutdownApplication),
                        },
                    },
                },
            };

            _trayIcon.Clicked += (sender, args) => OpenTrashBin();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += UpdateToolTip;
            _timer.Start();

            // Initial update
            UpdateToolTip(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize tray icon: {ex.Message}");
        }
    }

    private void UpdateToolTip(object? sender, EventArgs e)
    {
        if (_trayIcon == null) return;

        long size = GetTrashBinSize();
        _trayIcon.ToolTipText = $"Trash Manager\nSize: {FormatBytes(size)}";
    }

    private static long GetTrashBinSize()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SHQUERYRBINFO pSHQueryRBInfo = new SHQUERYRBINFO();
                pSHQueryRBInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                int result = SHQueryRecycleBin(null, ref pSHQueryRBInfo);
                if (result == 0)
                {
                    return pSHQueryRBInfo.i64Size;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".Trash"));
            }
            else // Assuming Linux
            {
                return GetDirectorySize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Trash/files"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get trash size: {ex.Message}");
        }

        return 0;
    }

    private static long GetDirectorySize(string path)
    {
        long size = 0;
        try
        {
            var directory = new DirectoryInfo(path);
            if (directory.Exists)
            {
                foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
                {
                    size += file.Length;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating directory size for {path}: {ex.Message}");
        }
        return size;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
        if (bytes == 0)
            return "0" + suf[0];
        long absoluteBytes = Math.Abs(bytes);
        int place = Convert.ToInt32(Math.Floor(Math.Log(absoluteBytes, 1024)));
        double num = Math.Round(absoluteBytes / Math.Pow(1024, place), 1);
        return (Math.Sign(bytes) * num).ToString() + " " + suf[place];
    }


    /// <summary>
    /// Opens the trash
    /// </summary>
    public static void OpenTrashBin()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("explorer.exe", "shell:RecycleBinFolder");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.Trash");
            }
            else
            {
                Process.Start("xdg-open", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.local/share/Trash");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open trash bin: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the trashbin
    /// </summary>
    public void ClearTrashBin()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SHEmptyRecycleBin(IntPtr.Zero, null, (uint)RecycleFlags.SHERB_NOCONFIRMATION);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                const string script = "tell application \"Finder\" to empty trash";
                Process.Start("osascript", $"-e '{script}'");
            }
            else
            {
                var trashPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Trash/files");
                var trashInfoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Trash/info");

                if (Directory.Exists(trashPath))
                {
                    Directory.Delete(trashPath, true);
                    Directory.CreateDirectory(trashPath);
                }

                if (Directory.Exists(trashInfoPath))
                {
                    Directory.Delete(trashInfoPath, true);
                    Directory.CreateDirectory(trashInfoPath);
                }
            }

            UpdateToolTip(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to clear trash bin: {ex.Message}");
        }
    }

    /// <summary>
    /// Shuts down the app
    /// </summary>
    public void ShutdownApplication()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}