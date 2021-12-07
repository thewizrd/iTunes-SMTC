using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if DEBUG || RELEASE
using Windows.ApplicationModel;
#endif

namespace iTunes.SMTC
{
    public sealed partial class MainWindow : Window
    {
        private void InitializeSettings()
        {
            TrackNotificationSwitch.IsOn = Settings.ShowTrackToast;
            TrackNotificationSwitch.Toggled += TrackNotificationSwitch_Toggled;
#if DEBUG || RELEASE
            StartupSwitch.Visibility = Visibility.Visible;
            StartupSwitch.IsOn = Settings.OpenOnStartup;
            StartupSwitch.Toggled += StartupSwitch_Toggled;
#endif
        }

#if DEBUG || RELEASE
        private async void StartupSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            var oldValue = Settings.OpenOnStartup;
            var newValue = !oldValue;

            StartupTask startupTask = await StartupTask.GetAsync("iTunes.SMTC");

            if (newValue)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }

            Settings.OpenOnStartup = newValue;
        }
#endif

        private void TrackNotificationSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.ShowTrackToast = !Settings.ShowTrackToast;
        }
    }
}
