using System;
using System.Diagnostics;
using System.IO;

namespace DesktopNotifications.FreeDesktop
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class FreeDesktopApplicationContext : ApplicationContext
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    {
        private FreeDesktopApplicationContext(string name, string? appIcon) : base(name)
        {
            AppIcon = appIcon;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string? AppIcon { get; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static FreeDesktopApplicationContext FromCurrentProcess(string? appIcon = null)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var mainModule = Process.GetCurrentProcess().MainModule;

            if (mainModule?.FileName == null)
            {
                throw new InvalidOperationException("No valid process module found.");
            }

            return new FreeDesktopApplicationContext(
                Path.GetFileNameWithoutExtension(mainModule.FileName),
                appIcon
            );
        }
    }
}