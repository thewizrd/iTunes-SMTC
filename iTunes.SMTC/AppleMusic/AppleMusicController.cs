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
                        UpdateSMTCDisplay(GetAMPlayerInfo());
                    }
                    else
                    {
                        // clear info
                    }
                }
                catch (Exception) { }
            };
            _statusTimer.Start();
        }

        private void UpdateSMTCDisplay(AMPlayerInfo info)
        {
            RunOnUIThread(() =>
            {
                if (info != null)
                {
                    _systemMediaTransportControls.PlaybackStatus = info.IsPlaying ? Windows.Media.MediaPlaybackStatus.Playing : (!string.IsNullOrEmpty(info.TrackData?.Name) ? MediaPlaybackStatus.Paused : MediaPlaybackStatus.Closed);
                    _systemMediaTransportControls.IsEnabled = !string.IsNullOrEmpty(info?.TrackData?.Name);

                    _systemMediaTransportControls.ShuffleEnabled = info.ShuffleEnabled;
                    _systemMediaTransportControls.AutoRepeatMode = info.RepeatMode;

                    _systemMediaTransportControls.IsPreviousEnabled = info.SkipBackEnabled;
                    _systemMediaTransportControls.IsNextEnabled = info.SkipForwardEnabled;

                    if (info.PlayPauseStopButtonState == PlayPauseStopButtonState.Stop)
                    {
                        _systemMediaTransportControls.IsPauseEnabled = false;
                        _systemMediaTransportControls.IsPlayEnabled = false;
                        _systemMediaTransportControls.IsStopEnabled = true;
                    }
                    else
                    {
                        _systemMediaTransportControls.IsPauseEnabled = true;
                        _systemMediaTransportControls.IsPlayEnabled = true;
                        _systemMediaTransportControls.IsStopEnabled = false;
                    }

                    if (_currentTrack == null || !Equals(info.TrackData, _currentTrack))
                    {
                        _currentTrack = info.TrackData;

                        SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                        updater.ClearAll();

                        if (!string.IsNullOrEmpty(info?.TrackData?.Name))
                        {
                            updater.Type = MediaPlaybackType.Music;
                            updater.MusicProperties.Title = info.TrackData.Name;
                            updater.MusicProperties.Artist = info.TrackData.Artist;
                            updater.MusicProperties.AlbumTitle = info.TrackData.Album;
                            _metadataEmpty = false;
                        }
                        else
                        {
                            _metadataEmpty = true;
                        }

                        updater.Update();
                    }
                }
                else
                {
                    _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    _systemMediaTransportControls.IsEnabled = false;
                    _systemMediaTransportControls.ShuffleEnabled = false;
                    _systemMediaTransportControls.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;

                    _systemMediaTransportControls.IsPauseEnabled = true;
                    _systemMediaTransportControls.IsPlayEnabled = true;
                    _systemMediaTransportControls.IsStopEnabled = false;

                    SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                    updater.ClearAll();
                    _metadataEmpty = true;

                    _systemMediaTransportControls.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties());
                }
            });
        }

        private static bool IsAppleMusicRunning()
        {
            Process[] processes = Process.GetProcessesByName("AppleMusic");

            return processes.Length > 0;
        }
    }
}
