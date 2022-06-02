using iTunes.SMTC.Utils;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
#if DEBUG || RELEASE
using Windows.ApplicationModel;
#endif
using Windows.System;

namespace iTunes.SMTC
{
    public partial class SettingsUi : Form
    {
        public SettingsUi()
        {
            InitializeComponent();

            Text = "iTunes MediaController Settings";
#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
            VersionCodeText.Text = "v" + (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version.ToString(4);
#else
            VersionCodeText.Text = string.Format(CultureInfo.InvariantCulture, "v{0}.{1}.{2}.{3}",
                Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
#endif

            InitializeSettings();

            iTunesDispatcherCtrl = DispatcherQueueController.CreateOnDedicatedThread();
            iTunesDispatcher = iTunesDispatcherCtrl.DispatcherQueue;

            InitializeSMTC();
            InitializeiTunesController();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.MinimizeToTray();
        }

        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.MinimizeToTray();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.MinimizeToTray();
            }
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
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
                
                components?.Dispose();
                _currentTrack?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            if (_iTunesApp != null)
            {
                Marshal.FinalReleaseComObject(_iTunesApp);
            }
            // TODO: set large fields to null
            _iTunesApp = null;

            base.Dispose(disposing);
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~SettingsUi()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        protected void RunOnUIThread(Action action)
        {
            this.FormTitle.Invoke(action);
        }
    }
}