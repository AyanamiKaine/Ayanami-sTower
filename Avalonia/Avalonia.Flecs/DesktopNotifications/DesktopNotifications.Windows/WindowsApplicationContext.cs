using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace DesktopNotifications.Windows
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class WindowsApplicationContext : ApplicationContext
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private WindowsApplicationContext(string name, string appUserModelId) : base(name)
        {
            AppUserModelId = appUserModelId;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string AppUserModelId { get; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID(
            [MarshalAs(UnmanagedType.LPWStr)] string appId);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static WindowsApplicationContext FromCurrentProcess(
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
            string? customName = null,
            string? appUserModelId = null)
        {
            var mainModule = Process.GetCurrentProcess().MainModule;

            if (mainModule?.FileName == null)
            {
                throw new InvalidOperationException("No valid process module found.");
            }

            var appName = customName ?? Path.GetFileNameWithoutExtension(mainModule.FileName);
            var aumid = appUserModelId ?? appName; //TODO: Add seeded bits to avoid collisions?

            SetCurrentProcessExplicitAppUserModelID(aumid);

            using var shortcut = new ShellLink
            {
                TargetPath = mainModule.FileName,
                Arguments = string.Empty,
                AppUserModelID = aumid
            };

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var startMenuPath = Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs");
            var shortcutFile = Path.Combine(startMenuPath, $"{appName}.lnk");

            shortcut.Save(shortcutFile);

            return new WindowsApplicationContext(appName, aumid);
        }
    }
}