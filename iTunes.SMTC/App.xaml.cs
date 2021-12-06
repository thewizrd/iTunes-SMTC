using Hardcodet.Wpf.TaskbarNotification;
using iTunes.SMTC.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Windows.Controls;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace iTunes.SMTC
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;

        private MainWindow m_window;
        private Window _MainWindow
        {
            get
            {
                if (m_window == null)
                {
                    m_window = new MainWindow();
                }

                return m_window;
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();

#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
            AppSettings.Default.Upgrade();
#endif
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            const string appName = "iTunes.SMTC";

            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                //app is already running! Exiting the application  
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                return;
            }

            // Start app with window hidden
            _MainWindow.Hide();
        }
    }
}
