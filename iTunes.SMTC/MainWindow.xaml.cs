using iTunes.SMTC.Utils;
using iTunesLib;
using Microsoft.UI;
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

        private AppWindow _AppWindow;

        public MainWindow()
        {
            InitializeComponent();

            Title = "iTunes MediaController Settings";

            this.LoadIcon(ICO_PATH);
            this.CreateTaskBarIcon();

            // Set window attributes
            _AppWindow = this.GetAppWindow();
            // Disable resizing and remove min/max window buttons
            _AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            _AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 360));
            this.PlacementCenterWindowInMonitorWin32();
            // Setup titlebar
            _AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            _AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            _AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            _AppWindow.Closing += _AppWindow_Closing;
            _AppWindow.Changed += MainAppWindow_Changed;

            InitializeSettings();

            iTunesDispatcherCtrl = DispatcherQueueController.CreateOnDedicatedThread();
            iTunesDispatcher = iTunesDispatcherCtrl.DispatcherQueue;

            InitializeSMTC();
            InitializeiTunesController();
        }

        // Override closing event
        private void _AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            // Disable closing application; hide to tray area
            args.Cancel = true;
            this.Hide();
        }

        private void MainAppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange && sender.TitleBar.ExtendsContentIntoTitleBar)
            {
                // Need to update our drag region if the size of the window changes
                SetDragRegionForCustomTitleBar(sender);
            }
        }

        private void SetDragRegionForCustomTitleBar(AppWindow appWindow)
        {
            //Infer titlebar height
            int titleBarHeight = appWindow.TitleBar.Height;
            AppTitleBar.Height = titleBarHeight;

            // Get caption button occlusion information
            // Use LeftInset if you've explicitly set your window layout to RTL or if app language is a RTL language
            int CaptionButtonOcclusionWidth = appWindow.TitleBar.RightInset;

            // Define your drag Regions
            int windowIconWidthAndPadding = (int)AppIcon.ActualWidth + (int)AppIcon.Margin.Right;
            int dragRegionWidth = appWindow.Size.Width - (CaptionButtonOcclusionWidth + windowIconWidthAndPadding);

            Windows.Graphics.RectInt32[] dragRects = new Windows.Graphics.RectInt32[] { };
            Windows.Graphics.RectInt32 dragRect;

            dragRect.X = windowIconWidthAndPadding;
            dragRect.Y = 0;
            dragRect.Height = titleBarHeight;
            dragRect.Width = dragRegionWidth;

            appWindow.TitleBar.SetDragRectangles(dragRects.Append(dragRect).ToArray());
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
                        taskbarIcon = null;
                    }
                    if (_mediaPlayer != null)
                    {
                        _systemMediaTransportControls = null;
                        _mediaPlayer.Dispose();
                        _mediaPlayer = null;
                    }
                    _delayStartTimer?.Stop();
                    _statusTimer?.Stop();

                    _delayStartTimer?.Dispose();
                    _statusTimer?.Dispose();

                    iTunesDispatcherCtrl.ShutdownQueueAsync();
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
