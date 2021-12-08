using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if DEBUG || RELEASE
using Windows.Storage;
#endif

namespace iTunes.SMTC
{
    public static class Settings
    {
        public static bool ShowTrackToast { get { return GetShowTrackToast(); } set { SetShowTrackToast(value); } }
        public static bool OpenOnStartup { get { return GetOpenOnStartup(); } set { SetOpenOnStartup(value); } }

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
#else
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        #region Settings Keys
        private const string KEY_SHOWTRACKTOAST = "key_showtracktoast";
        private const string KEY_OPENONSTARTUP = "key_openonstartup";
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
#endif
    }
}
