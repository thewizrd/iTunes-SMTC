namespace iTunes.SMTC.Utils
{
    public static class FormUtils
    {
        public static void BringToForeground(this Form form)
        {
            form.WindowState = FormWindowState.Normal;
            form.Show();
            form.Activate();
            form.ShowInTaskbar = true;
        }

        public static void MinimizeToTray(this Form form)
        {
            form.ShowInTaskbar = false;
            form.Hide();
        }
    }
}
