using iTunes.SMTC.AppleMusic.Model;
using System.Diagnostics;
using Windows.Media;
using Windows.System;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController : BaseController
    {
        private bool _isPlaying = false;
        private bool _metadataEmpty = true;
        private TrackMetadata _currentTrack;

        private readonly DispatcherQueueController AMDispatcherCtrl;
        private readonly DispatcherQueue AMDispatcher;

        private DispatcherQueueTimer _statusTimer;

        public override string Key => "AMPreview";
        public override bool IsEnabled => Settings.EnableAppleMusicController;

        public AppleMusicController() : base()
        {
            AMDispatcherCtrl = DispatcherQueueController.CreateOnDedicatedThread();
            AMDispatcher = AMDispatcherCtrl.DispatcherQueue;
        }

        protected override string GetNotificationTag() => "AMPreview.SMTC";

        public override void Initialize()
        {
            base.Initialize();
            InitializeAMController();
        }

        public override void Destroy()
        {
            StopNPSMService();
            _statusTimer?.Stop();

            _isPlaying = false;
            _metadataEmpty = true;

            if (_systemMediaTransportControls != null)
            {
                _systemMediaTransportControls.IsEnabled = false;
            }

            base.Destroy();
        }

        private void InitializeAMController()
        {
            _statusTimer = AMDispatcher.CreateTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(1);
            _statusTimer.Tick += (s, e) =>
            {
                try
                {
                    // Check if Apple Music is currently running
                    // TODO: check SMTC
                    if (IsAppleMusicRunning())
                    {
                        // Update SMTC display
                        if (MediaSession != null)
                        {
                            UpdateSMTCExtras(GetAMPlayerInfo());
                        }
                        else
                        {
                            UpdateSMTCDisplay(GetAMPlayerInfo());
                        }
                    }
                    else
                    {
                        // clear info
                    }
                }
                catch (Exception) { }
            };

            StartNPSMService();
        }

        private static bool IsAppleMusicRunning()
        {
            Process[] processes = Process.GetProcessesByName("AppleMusic");

            return processes.Length > 0;
        }

        public override void OnSystemControlsAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            // Apple Music Preview doesn't support changing repeat mode as of now
            // Fallback to FlaUI
            SendAMPlayerCommand(AppleMusicControlButtons.Repeat);
        }

        public override void OnSystemControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsPlayEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Play);
                    }
                    else
                    {
                        SendAMPlayerCommand(AppleMusicControlButtons.PlayPauseStop);
                    }
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsPauseEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Pause);
                    }
                    else
                    {
                        SendAMPlayerCommand(AppleMusicControlButtons.PlayPauseStop);
                    }
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsStopEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Stop);
                    }
                    else
                    {
                        SendAMPlayerCommand(AppleMusicControlButtons.PlayPauseStop);
                    }
                    break;
                case SystemMediaTransportControlsButton.Next:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsNextEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Next);
                    }
                    else
                    {
                        SendAMPlayerCommand(AppleMusicControlButtons.SkipForward);
                    }
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsPreviousEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Previous);
                    }
                    else
                    {
                        SendAMPlayerCommand(AppleMusicControlButtons.SkipBack);
                    }
                    break;
            }
        }

        public override void OnSystemControlsPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            // Apple Music Preview doesn't support changing playback position natively as of now
            // Fallback to FlaUI
            UpdateAMPlayerPlaybackPosition(args.RequestedPlaybackPosition);
        }

        public override void OnSystemControlsShuffleEnabledChangeRequested(SystemMediaTransportControls sender, ShuffleEnabledChangeRequestedEventArgs args)
        {
            // Apple Music Preview doesn't support changing shuffle mode as of now
            // Fallback to FlaUI
            SendAMPlayerCommand(AppleMusicControlButtons.Shuffle);
        }
    }
}
