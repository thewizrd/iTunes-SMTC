#if DEBUG || RELEASE
using Windows.Storage;
#endif

namespace iTunes.SMTC
{
    public static class Settings
    {
        public static bool ShowTrackToast { get { return GetShowTrackToast(); } set { SetShowTrackToast(value); } }
        public static bool OpenOnStartup { get { return GetOpenOnStartup(); } set { SetOpenOnStartup(value); } }
        public static bool EnableCrashReporting { get { return GetEnableCrashReporting(); } set { SetEnableCrashReporting(value); } }
        public static bool EnableiTunesController { get { return GetEnableiTunesController(); } set { SetEnableiTunesController(value); } }
        public static bool EnableAppleMusicController { get { return GetEnableAMPreviewController(); } set { SetEnableAMPreviewController(value); } }

#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
        private static bool GetShowTrackToast()
        {
            return Properties.Settings.Default.ShowTrackToast;
        }

        private static void SetShowTrackToast(bool value)
        {
            Properties.Settings.Default.ShowTrackToast = value;
            Properties.Settings.Default.Save();
        }

        private static bool GetOpenOnStartup()
        {
            return Properties.Settings.Default.OpenOnStartup;
        }

        private static void SetOpenOnStartup(bool value)
        {
            Properties.Settings.Default.OpenOnStartup = value;
            Properties.Settings.Default.Save();
        }

        private static bool GetEnableCrashReporting()
        {
            return Properties.Settings.Default.EnableCrashReporting;
        }

        private static void SetEnableCrashReporting(bool value)
        {
            Properties.Settings.Default.EnableCrashReporting = value;
            Properties.Settings.Default.Save();
        }

        private static bool GetEnableiTunesController()
        {
            return Properties.Settings.Default.EnableiTunesController;
        }

        private static void SetEnableiTunesController(bool value)
        {
            Properties.Settings.Default.EnableiTunesController = value;
            Properties.Settings.Default.Save();
        }

        private static bool GetEnableAMPreviewController()
        {
            return Properties.Settings.Default.EnableAMPreviewController;
        }

        private static void SetEnableAMPreviewController(bool value)
        {
            Properties.Settings.Default.EnableAMPreviewController = value;
            Properties.Settings.Default.Save();
        }
#else
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        #region Settings Keys
        private const string KEY_SHOWTRACKTOAST = "key_showtracktoast";
        private const string KEY_OPENONSTARTUP = "key_openonstartup";
        private const string KEY_ENABLECRASHREPORT = "key_enablecrashreport";
        private const string KEY_ENABLEITUNESCTRLR = "key_enableitunesctrlr";
        private const string KEY_ENABLEAMPREVIEWCTRLR = "key_enableampreviewctrlr";
        #endregion Settings Keys

        private static bool GetShowTrackToast()
        {
            if (LocalSettings.Values.TryGetValue(KEY_SHOWTRACKTOAST, out object value))
            {
                return (bool)value;
            }

            return false;
        }

        private static void SetShowTrackToast(bool value)
        {
            LocalSettings.Values[KEY_SHOWTRACKTOAST] = value;
        }

        private static bool GetOpenOnStartup()
        {
            if (LocalSettings.Values.TryGetValue(KEY_OPENONSTARTUP, out object value))
            {
                return (bool)value;
            }

            return false;
        }

        private static void SetOpenOnStartup(bool value)
        {
            LocalSettings.Values[KEY_OPENONSTARTUP] = value;
        }

        private static bool GetEnableCrashReporting()
        {
            if (LocalSettings.Values.TryGetValue(KEY_ENABLECRASHREPORT, out object value))
            {
                return (bool)value;
            }

            return true;
        }

        private static void SetEnableCrashReporting(bool value)
        {
            LocalSettings.Values[KEY_ENABLECRASHREPORT] = value;
        }

        private static bool GetEnableiTunesController()
        {
            if (LocalSettings.Values.TryGetValue(KEY_ENABLEITUNESCTRLR, out object value))
            {
                return (bool)value;
            }

            return true;
        }

        private static void SetEnableiTunesController(bool value)
        {
            LocalSettings.Values[KEY_ENABLEITUNESCTRLR] = value;
        }

        private static bool GetEnableAMPreviewController()
        {
            if (LocalSettings.Values.TryGetValue(KEY_ENABLEAMPREVIEWCTRLR, out object value))
            {
                return (bool)value;
            }

            return false;
        }

        private static void SetEnableAMPreviewController(bool value)
        {
            LocalSettings.Values[KEY_ENABLEAMPREVIEWCTRLR] = value;
        }
#endif
    }
}
