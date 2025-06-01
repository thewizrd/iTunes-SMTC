using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTunes.SMTC.AppleMusic.Model
{
#nullable enable
    public sealed class AMWindow : IDisposable
    {
        private FlaUI.Core.Application? application;
        private UIA3Automation? automation;
        private Window? window;
        
        public Window? Window
        {
            get
            {
                if (window == null)
                {
                    FindAppleMusicWindow();
                }

                return window;
            }
        }

        private bool disposedValue;

        public static AMWindow GetWindow()
        {
            return new AMWindow();
        }

        private AMWindow() 
        {
            FindAppleMusicWindow();
        }

        private void FindAppleMusicWindow()
        {
            if (window == null)
            {
                try
                {
                    var processes = Process.GetProcessesByName("AppleMusic");

                    // Check if app window is available and responding
                    var process = processes.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero && p.Responding);

                    if (process != null)
                    {
                        application = FlaUI.Core.Application.Attach(process);

                        if (Wait.UntilResponsive(application.MainWindowHandle, TimeSpan.FromSeconds(5)))
                        {
                            automation = new UIA3Automation();
                            var window = application.GetMainWindow(automation, waitTimeout: TimeSpan.FromSeconds(5));

                            if ((window?.Name == "Apple Music" && window.ClassName == "WinUIDesktopWin32WindowClass") ||
                                window?.Name == "MiniPlayer" || window?.Name == "Mini Player")
                            {
                                this.window = window;
                            }
                        }
                    }
                }
                catch (TimeoutException) { }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    automation?.Dispose();
                    application?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                automation = null;
                application = null;
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AMWindow()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
#nullable restore
}
