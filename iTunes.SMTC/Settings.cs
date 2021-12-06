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

        public static bool ShowTrackToast { get { return GetShowTrackToast(); } set { SetShowTrackToast(value); } }

        #region Settings Keys
        private const string KEY_SHOWTRACKTOAST = "key_showtracktoast";
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
    }
}
