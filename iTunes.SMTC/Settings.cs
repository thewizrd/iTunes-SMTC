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
        public static bool ShowTrackToast { get { return GetShowTrackToast(); } set { SetShowTrackToast(value); } }

#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
        private static bool GetShowTrackToast()
        {
            return AppSettings.Default.ShowTrackToast;
        }

        private static void SetShowTrackToast(bool value)
        {
            AppSettings.Default.ShowTrackToast = value;
        }
#else
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

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
#endif
    }

#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.10.0.0")]
    internal sealed partial class AppSettings : global::System.Configuration.ApplicationSettingsBase
    {

        private static AppSettings defaultInstance = ((AppSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new AppSettings())));

        public static AppSettings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("false")]
        public bool ShowTrackToast
        {
            get
            {
                return ((bool)(this["ShowTrackToast"]));
            }
            set
            {
                this["ShowTrackToast"] = value;
                this.Save();
            }
        }

        public override void Save()
        {
            base.Save();
        }
    }
#endif
}
