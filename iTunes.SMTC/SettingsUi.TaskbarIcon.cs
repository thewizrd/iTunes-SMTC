using iTunes.SMTC.Utils;

namespace iTunes.SMTC
{
    public partial class SettingsUi
    {
        private void TaskbarIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.BringToForeground();
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            this.BringToForeground();
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
