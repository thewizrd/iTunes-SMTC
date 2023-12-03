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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsUi));
            TrackNotificationSwitch = new CheckBox();
            FormTitle = new Label();
            StartupSwitch = new CheckBox();
            TaskbarIcon = new NotifyIcon(components);
            TaskbarIconCtxMenu = new ContextMenuStrip(components);
            OpenMenuItem = new ToolStripMenuItem();
            QuitMenuItem = new ToolStripMenuItem();
            VersionCodeText = new Label();
            CrashReportSwitch = new CheckBox();
            PlayerPluginsLabel = new Label();
            iTunesSwitch = new CheckBox();
            AppleMusicSwitch = new CheckBox();
            TaskbarIconCtxMenu.SuspendLayout();
            SuspendLayout();
            // 
            // TrackNotificationSwitch
            // 
            TrackNotificationSwitch.AutoSize = true;
            TrackNotificationSwitch.Location = new Point(19, 52);
            TrackNotificationSwitch.Margin = new Padding(10, 5, 10, 5);
            TrackNotificationSwitch.Name = "TrackNotificationSwitch";
            TrackNotificationSwitch.Size = new Size(148, 19);
            TrackNotificationSwitch.TabIndex = 0;
            TrackNotificationSwitch.Text = "Show track notification";
            TrackNotificationSwitch.UseVisualStyleBackColor = true;
            // 
            // FormTitle
            // 
            FormTitle.AutoSize = true;
            FormTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Regular, GraphicsUnit.Point);
            FormTitle.Location = new Point(9, 9);
            FormTitle.Margin = new Padding(0, 0, 0, 10);
            FormTitle.Name = "FormTitle";
            FormTitle.Size = new Size(86, 28);
            FormTitle.TabIndex = 1;
            FormTitle.Text = "Settings";
            // 
            // StartupSwitch
            // 
            StartupSwitch.AutoSize = true;
            StartupSwitch.Location = new Point(19, 81);
            StartupSwitch.Margin = new Padding(10, 5, 10, 5);
            StartupSwitch.Name = "StartupSwitch";
            StartupSwitch.Size = new Size(127, 19);
            StartupSwitch.TabIndex = 2;
            StartupSwitch.Text = "Run app on startup";
            StartupSwitch.UseVisualStyleBackColor = true;
            // 
            // TaskbarIcon
            // 
            TaskbarIcon.ContextMenuStrip = TaskbarIconCtxMenu;
            TaskbarIcon.Icon = (Icon)resources.GetObject("TaskbarIcon.Icon");
            TaskbarIcon.Text = "Media Controller";
            TaskbarIcon.Visible = true;
            TaskbarIcon.MouseDoubleClick += TaskbarIcon_MouseDoubleClick;
            // 
            // TaskbarIconCtxMenu
            // 
            TaskbarIconCtxMenu.Items.AddRange(new ToolStripItem[] { OpenMenuItem, QuitMenuItem });
            TaskbarIconCtxMenu.Name = "TaskbarIconCtxMenu";
            TaskbarIconCtxMenu.Size = new Size(104, 48);
            // 
            // OpenMenuItem
            // 
            OpenMenuItem.Name = "OpenMenuItem";
            OpenMenuItem.Size = new Size(103, 22);
            OpenMenuItem.Text = "Open";
            OpenMenuItem.Click += OpenMenuItem_Click;
            // 
            // QuitMenuItem
            // 
            QuitMenuItem.Name = "QuitMenuItem";
            QuitMenuItem.Size = new Size(103, 22);
            QuitMenuItem.Text = "Exit";
            QuitMenuItem.Click += QuitMenuItem_Click;
            // 
            // VersionCodeText
            // 
            VersionCodeText.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            VersionCodeText.Location = new Point(201, 239);
            VersionCodeText.Margin = new Padding(0);
            VersionCodeText.Name = "VersionCodeText";
            VersionCodeText.Size = new Size(83, 23);
            VersionCodeText.TabIndex = 3;
            VersionCodeText.Text = "v1.0.0.0";
            VersionCodeText.TextAlign = ContentAlignment.MiddleRight;
            // 
            // CrashReportSwitch
            // 
            CrashReportSwitch.AutoSize = true;
            CrashReportSwitch.Location = new Point(19, 110);
            CrashReportSwitch.Margin = new Padding(10, 5, 10, 5);
            CrashReportSwitch.Name = "CrashReportSwitch";
            CrashReportSwitch.Size = new Size(144, 19);
            CrashReportSwitch.TabIndex = 4;
            CrashReportSwitch.Text = "Enable crash reporting";
            CrashReportSwitch.UseVisualStyleBackColor = true;
            // 
            // PlayerPluginsLabel
            // 
            PlayerPluginsLabel.AutoSize = true;
            PlayerPluginsLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular, GraphicsUnit.Point);
            PlayerPluginsLabel.Location = new Point(9, 144);
            PlayerPluginsLabel.Margin = new Padding(0, 10, 0, 10);
            PlayerPluginsLabel.Name = "PlayerPluginsLabel";
            PlayerPluginsLabel.Size = new Size(122, 19);
            PlayerPluginsLabel.TabIndex = 5;
            PlayerPluginsLabel.Text = "Controller Plugins";
            // 
            // iTunesSwitch
            // 
            iTunesSwitch.AutoSize = true;
            iTunesSwitch.Checked = true;
            iTunesSwitch.CheckState = CheckState.Checked;
            iTunesSwitch.Location = new Point(19, 178);
            iTunesSwitch.Margin = new Padding(10, 5, 10, 5);
            iTunesSwitch.Name = "iTunesSwitch";
            iTunesSwitch.Size = new Size(60, 19);
            iTunesSwitch.TabIndex = 6;
            iTunesSwitch.Tag = "iTunes";
            iTunesSwitch.Text = "iTunes";
            iTunesSwitch.UseVisualStyleBackColor = true;
            // 
            // AppleMusicSwitch
            // 
            AppleMusicSwitch.AutoSize = true;
            AppleMusicSwitch.Location = new Point(19, 207);
            AppleMusicSwitch.Margin = new Padding(10, 5, 10, 5);
            AppleMusicSwitch.Name = "AppleMusicSwitch";
            AppleMusicSwitch.Size = new Size(144, 19);
            AppleMusicSwitch.TabIndex = 7;
            AppleMusicSwitch.Tag = "AMPreview";
            AppleMusicSwitch.Text = "Apple Music (Preview)";
            AppleMusicSwitch.UseVisualStyleBackColor = true;
            // 
            // SettingsUi
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(284, 261);
            Controls.Add(AppleMusicSwitch);
            Controls.Add(iTunesSwitch);
            Controls.Add(PlayerPluginsLabel);
            Controls.Add(CrashReportSwitch);
            Controls.Add(VersionCodeText);
            Controls.Add(StartupSwitch);
            Controls.Add(FormTitle);
            Controls.Add(TrackNotificationSwitch);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsUi";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Media Controller Settings";
            TaskbarIconCtxMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
        private Label PlayerPluginsLabel;
        private CheckBox iTunesSwitch;
        private CheckBox AppleMusicSwitch;
    }
}