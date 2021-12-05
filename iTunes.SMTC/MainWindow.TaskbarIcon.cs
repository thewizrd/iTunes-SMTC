using Hardcodet.Wpf.TaskbarNotification;
using iTunes.SMTC.Utils;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace iTunes.SMTC
{
    public sealed partial class MainWindow : Window
    {
        private TaskbarIcon taskbarIcon;

        private void CreateTaskBarIcon()
        {
            if (taskbarIcon == null)
            {
                taskbarIcon = new TaskbarIcon()
                {
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(GetAppIcoPath()),
                    ToolTipText = "iTunes MediaController"
                };
                taskbarIcon.TrayMouseDoubleClick += (s, e) =>
                {
                    this.BringToForeground();
                };

                var ctxMenu = new ContextMenu();

                var openMenuItem = new MenuItem()
                {
                    Header = "Open"
                };
                openMenuItem.Click += (s, e) =>
                {
                    this.BringToForeground();
                };
                ctxMenu.Items.Add(openMenuItem);

                var quitMenuItem = new MenuItem()
                {
                    Header = "Quit"
                };
                quitMenuItem.Click += (s, e) =>
                {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                };
                ctxMenu.Items.Add(quitMenuItem);

                taskbarIcon.ContextMenu = ctxMenu;
            }
        }

        private string GetAppIcoPath()
        {
            var BaseDir = AppDomain.CurrentDomain.BaseDirectory;
            var AppIcoPath = System.IO.Path.Combine(BaseDir, @"Resources\App.ico");
            return AppIcoPath;
        }
    }
}
