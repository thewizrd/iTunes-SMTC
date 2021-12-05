using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace iTunes.SMTC
{
    public static class Settings
    {
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        public static bool StartMinimized { get { return GetStartMinimized(); } set { SetStartMinimized(value); } }
        public static bool MinimizeToTray { get { return GetMinimizeToTray(); } set { SetMinimizeToTray(value); } }

        #region Settings Keys
        private const string KEY_STARTMINIMIZED = "key_startminimized";
        private const string KEY_MINIMIZETOTRAY = "key_minimizetotray";
        #endregion Settings Keys

        private static bool GetStartMinimized()
        {
            if (LocalSettings.Values.TryGetValue(KEY_STARTMINIMIZED, out object value))
            {
                return (bool)value;
            }

            return false;
        }

        private static void SetStartMinimized(bool value)
        {
            LocalSettings.Values[KEY_STARTMINIMIZED] = value;
        }

        private static bool GetMinimizeToTray()
        {
            if (LocalSettings.Values.TryGetValue(KEY_MINIMIZETOTRAY, out object value))
            {
                return (bool)value;
            }

            return true;
        }

        private static void SetMinimizeToTray(bool value)
        {
            LocalSettings.Values[KEY_MINIMIZETOTRAY] = value;
        }
    }
}
