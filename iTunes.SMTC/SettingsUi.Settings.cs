using Microsoft.AppCenter.Crashes;
#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
using Microsoft.Win32.TaskScheduler;
using System.Reflection;
using System.Security.Principal;
#elif DEBUG || RELEASE
using Windows.ApplicationModel;
#endif

namespace iTunes.SMTC
{
    partial class SettingsUi
    {
        private const string TASK_NAME = "iTunes.SMTC";

#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
        private string GetTaskName()
        {
            return $"{TASK_NAME} {WindowsIdentity.GetCurrent().Name.Replace(@"\", "")}";
        }
#endif

        private void InitializeSettings()
        {
            TrackNotificationSwitch.Checked = Settings.ShowTrackToast;
            TrackNotificationSwitch.CheckedChanged += TrackNotificationSwitch_CheckedChanged;
            StartupSwitch.Checked = Settings.OpenOnStartup;
            StartupSwitch.CheckedChanged += StartupSwitch_CheckedChanged;
            CrashReportSwitch.Checked = Settings.EnableCrashReporting;
            CrashReportSwitch.CheckedChanged += CrashReportSwitch_CheckedChanged;
            iTunesSwitch.Checked = Settings.EnableiTunesController;
            iTunesSwitch.CheckedChanged += iTunesSwitch_CheckedChanged;
            AppleMusicSwitch.Checked = Settings.EnableAppleMusicController;
            AppleMusicSwitch.CheckedChanged += AppleMusicSwitch_CheckedChanged;
        }

        private async void StartupSwitch_CheckedChanged(object sender, EventArgs e)
        {
            var oldValue = Settings.OpenOnStartup;
            var newValue = !oldValue;

#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
            var ts = TaskService.Instance;

            var userId = WindowsIdentity.GetCurrent().Name;
            var taskName = GetTaskName();

            var task = ts.GetTask(taskName);
            if (task != null)
            {
                ts.RootFolder.DeleteTask(taskName);
            }

            if (newValue)
            {
                var exeName = Assembly.GetExecutingAssembly().GetName().Name;
                var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName + ".exe");

                var td = ts.NewTask();
                td.Triggers.Add(new LogonTrigger()
                {
                    UserId = userId
                });

                td.Actions.Add(exePath);

                ts.RootFolder.RegisterTaskDefinition(taskName, td);
            }

            Settings.OpenOnStartup = newValue;
#else
            StartupTask startupTask = await StartupTask.GetAsync(TASK_NAME);

            if (newValue)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }

            Settings.OpenOnStartup = newValue;
#endif
        }

        private void TrackNotificationSwitch_CheckedChanged(object sender, EventArgs e)
        {
            Settings.ShowTrackToast = !Settings.ShowTrackToast;
        }

        private async void CrashReportSwitch_CheckedChanged(object sender, EventArgs e)
        {
            var newValue = Settings.EnableCrashReporting = !Settings.EnableCrashReporting;
            await Crashes.SetEnabledAsync(newValue);
        }

        private void iTunesSwitch_CheckedChanged(object sender, EventArgs e)
        {
            var newValue = Settings.EnableiTunesController = !Settings.EnableiTunesController;
            var key = iTunesSwitch.Tag as string;
            EnableController(key, newValue);
        }

        private void AppleMusicSwitch_CheckedChanged(object sender, EventArgs e)
        {
            var newValue = Settings.EnableAppleMusicController = !Settings.EnableAppleMusicController;
            var key = AppleMusicSwitch.Tag as string;
            EnableController(key, newValue);
        }
    }
}
