namespace iTunes.SMTC
{
    partial class SettingsUi
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsUi));
            this.TrackNotificationSwitch = new System.Windows.Forms.CheckBox();
            this.FormTitle = new System.Windows.Forms.Label();
            this.StartupSwitch = new System.Windows.Forms.CheckBox();
            this.TaskbarIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.TaskbarIconCtxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.OpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.QuitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.VersionCodeText = new System.Windows.Forms.Label();
            this.CrashReportSwitch = new System.Windows.Forms.CheckBox();
            this.TaskbarIconCtxMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // TrackNotificationSwitch
            // 
            this.TrackNotificationSwitch.AutoSize = true;
            this.TrackNotificationSwitch.Location = new System.Drawing.Point(19, 72);
            this.TrackNotificationSwitch.Margin = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.TrackNotificationSwitch.Name = "TrackNotificationSwitch";
            this.TrackNotificationSwitch.Size = new System.Drawing.Size(148, 19);
            this.TrackNotificationSwitch.TabIndex = 0;
            this.TrackNotificationSwitch.Text = "Show track notification";
            this.TrackNotificationSwitch.UseVisualStyleBackColor = true;
            // 
            // FormTitle
            // 
            this.FormTitle.AutoSize = true;
            this.FormTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormTitle.Location = new System.Drawing.Point(9, 4);
            this.FormTitle.Margin = new System.Windows.Forms.Padding(0, 15, 0, 15);
            this.FormTitle.Name = "FormTitle";
            this.FormTitle.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            this.FormTitle.Size = new System.Drawing.Size(86, 48);
            this.FormTitle.TabIndex = 1;
            this.FormTitle.Text = "Settings";
            // 
            // StartupSwitch
            // 
            this.StartupSwitch.AutoSize = true;
            this.StartupSwitch.Location = new System.Drawing.Point(19, 101);
            this.StartupSwitch.Margin = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.StartupSwitch.Name = "StartupSwitch";
            this.StartupSwitch.Size = new System.Drawing.Size(127, 19);
            this.StartupSwitch.TabIndex = 2;
            this.StartupSwitch.Text = "Run app on startup";
            this.StartupSwitch.UseVisualStyleBackColor = true;
            // 
            // TaskbarIcon
            // 
            this.TaskbarIcon.ContextMenuStrip = this.TaskbarIconCtxMenu;
            this.TaskbarIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("TaskbarIcon.Icon")));
            this.TaskbarIcon.Text = "Media Controller for iTunes";
            this.TaskbarIcon.Visible = true;
            this.TaskbarIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.TaskbarIcon_MouseDoubleClick);
            // 
            // TaskbarIconCtxMenu
            // 
            this.TaskbarIconCtxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMenuItem,
            this.QuitMenuItem});
            this.TaskbarIconCtxMenu.Name = "TaskbarIconCtxMenu";
            this.TaskbarIconCtxMenu.Size = new System.Drawing.Size(104, 48);
            // 
            // OpenMenuItem
            // 
            this.OpenMenuItem.Name = "OpenMenuItem";
            this.OpenMenuItem.Size = new System.Drawing.Size(103, 22);
            this.OpenMenuItem.Text = "Open";
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // QuitMenuItem
            // 
            this.QuitMenuItem.Name = "QuitMenuItem";
            this.QuitMenuItem.Size = new System.Drawing.Size(103, 22);
            this.QuitMenuItem.Text = "Exit";
            this.QuitMenuItem.Click += new System.EventHandler(this.QuitMenuItem_Click);
            // 
            // VersionCodeText
            // 
            this.VersionCodeText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.VersionCodeText.Location = new System.Drawing.Point(201, 154);
            this.VersionCodeText.Margin = new System.Windows.Forms.Padding(0);
            this.VersionCodeText.Name = "VersionCodeText";
            this.VersionCodeText.Size = new System.Drawing.Size(83, 23);
            this.VersionCodeText.TabIndex = 3;
            this.VersionCodeText.Text = "v1.0.0.0";
            this.VersionCodeText.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // CrashReportSwitch
            // 
            this.CrashReportSwitch.AutoSize = true;
            this.CrashReportSwitch.Location = new System.Drawing.Point(19, 128);
            this.CrashReportSwitch.Margin = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.CrashReportSwitch.Name = "CrashReportSwitch";
            this.CrashReportSwitch.Size = new System.Drawing.Size(144, 19);
            this.CrashReportSwitch.TabIndex = 4;
            this.CrashReportSwitch.Text = "Enable crash reporting";
            this.CrashReportSwitch.UseVisualStyleBackColor = true;
            // 
            // SettingsUi
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 176);
            this.Controls.Add(this.CrashReportSwitch);
            this.Controls.Add(this.VersionCodeText);
            this.Controls.Add(this.StartupSwitch);
            this.Controls.Add(this.FormTitle);
            this.Controls.Add(this.TrackNotificationSwitch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsUi";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "iTunes MediaController Settings";
            this.TaskbarIconCtxMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CheckBox TrackNotificationSwitch;
        private Label FormTitle;
        private CheckBox StartupSwitch;
        private NotifyIcon TaskbarIcon;
        private ContextMenuStrip TaskbarIconCtxMenu;
        private ToolStripMenuItem OpenMenuItem;
        private ToolStripMenuItem QuitMenuItem;
        private Label VersionCodeText;
        private CheckBox CrashReportSwitch;
    }
}