using iTunes.SMTC.AppleMusic;
using iTunes.SMTC.iTunes;
using iTunes.SMTC.Utils;
using System.Linq;

namespace iTunes.SMTC
{
    public partial class SettingsUi
    {
        private readonly Dictionary<string, BaseController> ControllerRegistry = [];

        private void InitializeControllers()
        {
            ControllerRegistry.Add("iTunes", new iTunesController());
            ControllerRegistry.Add("AMPreview", new AppleMusicController());

            foreach (var entry in ControllerRegistry)
            {
                var controller = entry.Value;

                controller.ControllerInitialized += Controller_OnControllerInitialized;
                controller.ControllerDestroyed += Controller_OnControllerDestroyed;

                controller.EnableControllerIfAllowed();
            }
        }

        private void Controller_OnControllerInitialized(object sender, ControllerInitializedEventArgs e)
        {
            if (e is AMControllerInitializedEventArgs amArgs)
            {
                var serviceUri = amArgs.ServiceUri;

                TaskbarIconCtxMenu.Items.OfType<ToolStripItem>()
                    .Where(it => Equals(it.Tag, e.Key))
                    .ForEach(it => it.Visible = true);

                AMRemoteMenuItem.Text = $"AM Remote – {serviceUri.Host}:{serviceUri.Port:0000}";
            }
        }

        private void Controller_OnControllerDestroyed(object sender, ControllerDestroyedEventArgs e)
        {
            if (e.Key == "AMPreview")
            {
                TaskbarIconCtxMenu.Items.OfType<ToolStripItem>()
                    .Where(it => Equals(it.Tag, e.Key))
                    .ForEach(it => it.Visible = false);

                AMRemoteMenuItem.Text = null;
            }
        }

        private void EnableController(string controllerName, bool enable)
        {
            var controller = ControllerRegistry.GetValueOrDefault(controllerName);
            controller?.EnableController(enable);
        }
    }
}
