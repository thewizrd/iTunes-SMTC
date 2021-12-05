using iTunes.SMTC.Utils;
using iTunesLib;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace iTunes.SMTC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool disposedValue;
        private const string ICO_PATH = @"Resources\App.ico";

        public MainWindow()
        {
            InitializeComponent();

            Title = "iTunes MediaController Settings";
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            this.LoadIcon(ICO_PATH);
            this.SetWindowSize(600, 600);
            this.CreateTaskBarIcon();

            iTunesDispatcherCtrl = DispatcherQueueController.CreateOnDedicatedThread();
            iTunesDispatcher = iTunesDispatcherCtrl.DispatcherQueue;

            InitializeITunes();
            InitializeSMTC();
            IntializeEvents();

            InitializeControls(_iTunesApp.CurrentTrack);
        }

        private void ThrowIfDisposed()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(typeof(iTunesApp).FullName);
            }
        }
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (taskbarIcon != null)
                    {
                        taskbarIcon.Visibility = System.Windows.Visibility.Collapsed;
                        taskbarIcon.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                if (_iTunesApp != null)
                {
                    Marshal.ReleaseComObject(_iTunesApp);
                }
                // TODO: set large fields to null
                _iTunesApp = null;
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~MainWindow()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
