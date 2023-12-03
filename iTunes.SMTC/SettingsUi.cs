using iTunes.SMTC.Utils;
using System.Globalization;
#if DEBUG || RELEASE
using Windows.ApplicationModel;
#endif

namespace iTunes.SMTC
{
    public partial class SettingsUi : Form
    {
        public SettingsUi()
        {
            InitializeComponent();

            Text = "Media Controller Settings";
#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
            VersionCodeText.Text = "v" + (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version.ToString(4);
#else
            VersionCodeText.Text = string.Format(CultureInfo.InvariantCulture, "v{0}.{1}.{2}.{3}",
                Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
#endif

            InitializeSettings();
        }

        protected override void OnLoad(EventArgs e)
        {
            this.MinimizeToTray();
            RunOnUIThread(() =>
            {
                InitializeControllers();
            });
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
                foreach (var entry in ControllerRegistry)
                {
                    entry.Value.Destroy();
                    entry.Value.Dispose();
                }
                ControllerRegistry.Clear();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        protected void RunOnUIThread(Action action)
        {
            this.FormTitle.Invoke(action);
        }
    }
}