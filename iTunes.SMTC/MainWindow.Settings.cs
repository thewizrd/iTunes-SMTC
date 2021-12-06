using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTunes.SMTC
{
    public sealed partial class MainWindow : Window
    {
        private void InitializeSettings()
        {
            TrackNotificationSwitch.IsOn = Settings.ShowTrackToast;
            TrackNotificationSwitch.Toggled += TrackNotificationSwitch_Toggled;
        }

        private void TrackNotificationSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.ShowTrackToast = !Settings.ShowTrackToast;
        }
    }
}
