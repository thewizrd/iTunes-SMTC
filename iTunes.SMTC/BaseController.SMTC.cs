using Windows.Media;

namespace iTunes.SMTC
{
    public abstract partial class BaseController
    {
        private void InitializeSMTC()
        {
            _systemMediaTransportControls.IsEnabled = false;
            _systemMediaTransportControls.IsNextEnabled = true;
            _systemMediaTransportControls.IsPauseEnabled = true;
            _systemMediaTransportControls.IsPlayEnabled = true;
            _systemMediaTransportControls.IsPreviousEnabled = true;
            _systemMediaTransportControls.IsStopEnabled = true;
            _systemMediaTransportControls.IsRewindEnabled = true;
            _systemMediaTransportControls.IsFastForwardEnabled = true;
            _systemMediaTransportControls.ShuffleEnabled = false;
            _systemMediaTransportControls.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;
            _systemMediaTransportControls.ButtonPressed += SystemControls_ButtonPressed;
            _systemMediaTransportControls.ShuffleEnabledChangeRequested += SystemControls_ShuffleEnabledChangeRequested;
            _systemMediaTransportControls.AutoRepeatModeChangeRequested += SystemControls_AutoRepeatModeChangeRequested;
            _systemMediaTransportControls.PlaybackPositionChangeRequested += SystemControls_PlaybackPositionChangeRequested;
        }

        private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            OnSystemControlsButtonPressed(sender, args);
        }

        private void SystemControls_ShuffleEnabledChangeRequested(SystemMediaTransportControls sender, ShuffleEnabledChangeRequestedEventArgs args)
        {
            OnSystemControlsShuffleEnabledChangeRequested(sender, args);
        }

        private void SystemControls_AutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            OnSystemControlsAutoRepeatModeChangeRequested(sender, args);
        }

        private void SystemControls_PlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            OnSystemControlsPlaybackPositionChangeRequested(sender, args);
        }
    }
}
