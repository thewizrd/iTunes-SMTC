using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;

namespace iTunes.SMTC
{
    internal static class Program
    {
        private static Mutex _mutex = null;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            const string appName = "iTunes.SMTC";

            _mutex = new Mutex(true, appName, out bool createdNew);

            if (createdNew)
            {
                ApplicationConfiguration.Initialize();
                AppCenter.Start(Keys.AppCenterKey.GetSecret(), typeof(Crashes));
                Crashes.SetEnabledAsync(Settings.EnableCrashReporting);
                Application.Run(new SettingsUi());
            }
        }
    }
}