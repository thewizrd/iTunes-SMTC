using Microsoft.Extensions.DependencyInjection;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController
    {
        private void StartRemoteServer()
        {
            AMRemoteServer.Instance.Start((serviceCollection) =>
            {
                serviceCollection.AddSingleton(this);
            });
        }

        private void StopRemoteServer()
        {
            AMRemoteServer.Instance.Stop();
        }
    }
}
