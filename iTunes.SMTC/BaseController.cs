using Windows.Media;
using Windows.Media.Playback;

namespace iTunes.SMTC
{
    public abstract partial class BaseController : IDisposable
    {
        private bool disposedValue;

        private readonly MediaPlayer _mediaPlayer;
        protected readonly SystemMediaTransportControls _systemMediaTransportControls;

        protected BaseController()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.CommandManager.IsEnabled = false;
            _systemMediaTransportControls = _mediaPlayer.SystemMediaTransportControls;

            InitializeSMTC();
        }

        public abstract string Key { get; }
        public abstract bool IsEnabled { get; }

        public bool IsInitialized { get; private set; }
        public virtual void Initialize() { IsInitialized = true; }
        public virtual void Destroy() { IsInitialized = false; }

        public void EnableControllerIfAllowed()
        {
            EnableController(IsEnabled);
        }

        public void EnableController(bool enable)
        {
            if (enable)
            {
                if (!IsInitialized)
                {
                    Initialize();
                }
            }
            else
            {
                Destroy();
            }
        }

        protected abstract string GetNotificationTag();

        public virtual void OnSystemControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args) { }
        public virtual void OnSystemControlsShuffleEnabledChangeRequested(SystemMediaTransportControls sender, ShuffleEnabledChangeRequestedEventArgs args) { }
        public virtual void OnSystemControlsAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args) { }
        public virtual void OnSystemControlsPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args) { }

        protected void RunOnUIThread(Action action)
        {
            Task.Factory.StartNew(() =>
            {
                action?.Invoke();
            }, CancellationToken.None, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _mediaPlayer?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~BaseController()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
