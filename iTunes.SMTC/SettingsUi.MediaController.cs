using iTunes.SMTC.AppleMusic;
using iTunes.SMTC.iTunes;

namespace iTunes.SMTC
{
    public partial class SettingsUi
    {
        private readonly Dictionary<string, BaseController> ControllerRegistry = new();

        private void InitializeControllers()
        {
            ControllerRegistry.Add("iTunes", new iTunesController());
            ControllerRegistry.Add("AMPreview", new AppleMusicController());

            foreach (var entry in ControllerRegistry)
            {
                entry.Value.EnableControllerIfAllowed();
            }
        }

        private void EnableController(string controllerName, bool enable)
        {
            var controller = ControllerRegistry.GetValueOrDefault(controllerName);
            controller?.EnableController(enable);
        }
    }
}
