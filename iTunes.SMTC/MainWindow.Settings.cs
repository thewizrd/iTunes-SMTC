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
            StartMinimizedSwitch.IsOn = Settings.StartMinimized;
            StartMinimizedSwitch.Toggled += StartMinimizedSwitch_Toggled;

            MinimizeToTraySwitch.IsOn = Settings.MinimizeToTray;
            MinimizeToTraySwitch.Toggled += MinimizeToTraySwitch_Toggled;
        }

        private void StartMinimizedSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.StartMinimized = !Settings.StartMinimized;
        }

        private void MinimizeToTraySwitch_Toggled(object sender, RoutedEventArgs e)
        {
            Settings.MinimizeToTray = !Settings.MinimizeToTray;

            if (taskbarIcon != null)
            {
                taskbarIcon.Visibility = Settings.MinimizeToTray ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
        }
    }
}
