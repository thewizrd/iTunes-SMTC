using iTunes.SMTC.AppleMusic.Model;
using iTunes.SMTC.Utils;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using Windows.Media;
using Windows.Storage;
using Windows.System;
using Timer = System.Timers.Timer;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController : BaseController
    {
        private bool _isPlaying = false;
        private bool _metadataEmpty = false;
        private TrackMetadata _currentTrack;
        private VolumeState _currentVolume;

        private readonly DispatcherQueueController ArtworkDispatcherCtrl;
        private readonly DispatcherQueue ArtworkDispatcher;

        private Timer _statusTimer;

        public override string Key => "AMPreview";
        public override bool IsEnabled => Settings.EnableAppleMusicController;

        private Uri _artworkUri;

        private CancellationTokenSource toastCts = null;
        private CancellationTokenSource artworkCts = null;

        public AppleMusicController() : base()
        {
            ArtworkDispatcherCtrl = DispatcherQueueController.CreateOnDedicatedThread();
            ArtworkDispatcher = ArtworkDispatcherCtrl.DispatcherQueue;
        }

        protected override string GetNotificationTag() => "AMPreview.SMTC";

        public override void Initialize()
        {
            InitializeAMController();
            StartRemoteServer();
            base.Initialize();
        }

        public override void Destroy()
        {
            StopRemoteServer();
            StopNPSMService();
            StopNAudioService();
            _statusTimer?.Stop();

            _isPlaying = false;
            _metadataEmpty = true;

            if (_systemMediaTransportControls != null)
            {
                _systemMediaTransportControls.IsEnabled = false;
            }

            base.Destroy();
        }

        protected override void OnControllerInitialized(ControllerInitializedEventArgs e)
        {
            base.OnControllerInitialized(new AMControllerInitializedEventArgs()
            {
                Key = e.Key,
                ServiceUri = AMRemoteServer.Instance?.ServiceUri,
            });
        }

        private void InitializeAMController()
        {
            _statusTimer = new Timer()
            {
                AutoReset = true,
                Interval = 1000
            };
            _statusTimer.Elapsed += (s, e) =>
            {
                try
                {
                    // Check if Apple Music is currently running
                    if (IsAppleMusicRunning())
                    {
                        var playerInfo = GetAMPlayerInfo();

                        // Update SMTC display
                        UpdateSMTCDisplay(playerInfo);

                        if (UseMediaSession)
                        {
                            UpdateSMTCExtras(playerInfo);
                        }
                    }
                    else
                    {
                        // clear info
                        //_currentTrack?.Dispose();
                        _currentTrack = null;
                        _currentVolume = null;

                        if (_systemMediaTransportControls?.DisplayUpdater?.Type != MediaPlaybackType.Unknown)
                        {
                            UpdateSMTCDisplay(null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            };

            StartNPSMService();
            StartNAudioService();

            if (!_statusTimer.Enabled)
            {
                _statusTimer.Start();
            }
        }

        private static bool IsAppleMusicRunning()
        {
            Process[] processes = Process.GetProcessesByName("AppleMusic");

            return processes.Length > 0;
        }

        private void ResetToastToken()
        {
            toastCts?.Cancel();
            toastCts = new CancellationTokenSource();
        }

        private void ResetArtworkToken()
        {
            artworkCts?.Cancel();
            artworkCts = new CancellationTokenSource();
        }

        public override void OnSystemControlsAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            SendMediaCommand(AppleMusicControlButtons.Repeat);
        }

        public override void OnSystemControlsButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                case SystemMediaTransportControlsButton.Pause:
                case SystemMediaTransportControlsButton.Stop:
                    SendMediaCommand(AppleMusicControlButtons.PlayPauseStop);
                    break;
                case SystemMediaTransportControlsButton.Next:
                    SendMediaCommand(AppleMusicControlButtons.SkipForward);
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    SendMediaCommand(AppleMusicControlButtons.SkipBack);
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
            SendMediaCommand(AppleMusicControlButtons.Shuffle);
        }

        internal void SendMediaCommand(AppleMusicControlButtons command)
        {
            switch (command)
            {
                case AppleMusicControlButtons.SkipBack:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsPreviousEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Previous);
                    }
                    else
                    {
                        SendAMPlayerCommand(command);

                        if (PlayerStateChanged?.HasListeners() == true)
                        {
                            PlayerStateChanged?.Invoke(this, GetAMPlayerInfo().ToPlayerStateModel());
                        }
                    }
                    break;
                case AppleMusicControlButtons.PlayPauseStop:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsPlayEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Play);
                    }
                    else if (MediaPlaybackSource != null && _npsmInfo?.IsPauseEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Pause);
                    }
                    else if (MediaPlaybackSource != null && _npsmInfo?.IsStopEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Stop);
                    }
                    else
                    {
                        SendAMPlayerCommand(command);
                    }
                    break;
                case AppleMusicControlButtons.SkipForward:
                    if (MediaPlaybackSource != null && _npsmInfo?.IsNextEnabled == true)
                    {
                        MediaPlaybackSource?.SendMediaPlaybackCommand(NPSMLib.MediaPlaybackCommands.Next);
                    }
                    else
                    {
                        SendAMPlayerCommand(command);
                    }
                    break;
                // Apple Music Preview doesn't support changing repeat or shuffle mode as of now
                // Fallback to FlaUI
                case AppleMusicControlButtons.Shuffle:
                case AppleMusicControlButtons.Repeat:
                    SendAMPlayerCommand(command);

                    if (PlayerStateChanged?.HasListeners() == true)
                    {
                        PlayerStateChanged?.Invoke(this, GetAMPlayerInfo().ToPlayerStateModel());
                    }
                    break;
            }
        }

        internal async Task<PlayerStateModel> GetPlayerState(bool includeArtwork = false)
        {
            var playerInfo = GetAMPlayerInfo();

            if (playerInfo.TrackData != null)
            {
                if (includeArtwork)
                {
                    playerInfo.TrackData.Artwork = await GetArtwork();
                }
                return playerInfo.ToPlayerStateModel(includeArtwork);
            }
            else
            {
                return new PlayerStateModel()
                {
                    IsPlaying = false
                };
            }

            /*
            var player = GetMediaPlayer();

            if (player.SystemMediaTransportControls.PlaybackStatus == MediaPlaybackStatus.Playing || player.SystemMediaTransportControls.PlaybackStatus == MediaPlaybackStatus.Paused)
            {
                var musicProps = player.SystemMediaTransportControls.DisplayUpdater.MusicProperties;
                var artwork = includeArtwork ? await GetArtwork() : null;

                return new PlayerStateModel()
                {
                    IsPlaying = player.SystemMediaTransportControls.PlaybackStatus == MediaPlaybackStatus.Playing,
                    PlayPauseStopButtonState = player.SystemMediaTransportControls.PlaybackStatus == MediaPlaybackStatus.Playing ? PlayPauseStopButtonState.Pause : PlayPauseStopButtonState.Play,
                    ShuffleEnabled = player.SystemMediaTransportControls.ShuffleEnabled,
                    RepeatMode = player.SystemMediaTransportControls.AutoRepeatMode,
                    SkipBackEnabled = player.SystemMediaTransportControls.IsPreviousEnabled,
                    SkipForwardEnabled = player.SystemMediaTransportControls.IsNextEnabled,
                    TrackData = new TrackModel()
                    {
                        Name = musicProps.Title,
                        Artist = musicProps.Artist ?? musicProps.AlbumArtist,
                        Album = musicProps.AlbumTitle,
                        //Progress = (int)player.TimelineController.Position.TotalSeconds,
                        //Duration = (int)(player.TimelineController.Duration?.TotalSeconds ?? 0),
                    },
                    Artwork = artwork
                };
            }
            else
            {
                return new PlayerStateModel()
                {
                    IsPlaying = false
                };
            }
            */
        }

        internal async Task<byte[]> GetArtwork()
        {
            var player = GetMediaPlayer();

            var thumbnailRef = player?.SystemMediaTransportControls?.DisplayUpdater?.Thumbnail;

            if (thumbnailRef != null)
            {
                try
                {
                    using var stream = await thumbnailRef.OpenReadAsync();
                    using var roStream = stream.AsStreamForRead();

                    byte[] arr = new byte[roStream.Length];

                    await roStream.ReadAsync(arr);

                    return arr;
                }
                catch { }
            }

            return null;
        }

        private async Task<IStorageFile> SaveArtwork(Stream artworkStream)
        {
            if (_artworkUri == null)
            {
#if UNPACKAGEDDEBUG || UNPACKAGEDRELEASE
                var BasePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
                var AppPath = Path.Combine(BasePath, "iTunes.SMTC");
                Directory.CreateDirectory(AppPath);
                var FilePath = Path.Combine(AppPath, "artwork.img");
                _artworkUri = new Uri(FilePath);
#else
                var BaseFolder = ApplicationData.Current.LocalCacheFolder;
                var ArtworkFolderPath = Path.Combine(BaseFolder.Path, "Artwork");
                Directory.CreateDirectory(ArtworkFolderPath);
                var ArtworkFilePath = Path.Combine(ArtworkFolderPath, "artwork.img");
                _artworkUri = new Uri(ArtworkFilePath);
#endif

                try
                {
                    if (!File.Exists(_artworkUri.LocalPath))
                    {
                        File.Create(_artworkUri.LocalPath).Dispose();
                    }
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }

            var file = await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath);

            try
            {
                if (artworkStream != null && artworkStream.Length > 0)
                {
                    var fs = await file.OpenAsync(FileAccessMode.ReadWrite);
                    using var fsWrite = fs.AsStreamForWrite();
                    await artworkStream.CopyToAsync(fsWrite);
                    await fsWrite.FlushAsync();
                }
                else
                {
                    // Delete artwork or replace with empty
                    Properties.Resources.no_artwork.Save(_artworkUri.LocalPath);
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            return file;
        }

        private void ShowToastNotification(TrackMetadata track)
        {
            if (track != null)
            {
                ResetToastToken();
                var token = toastCts.Token;

                ArtworkDispatcher.TryEnqueue(DispatcherQueuePriority.Low, async () =>
                {
                    try
                    {
                        await Task.Delay(100, token);
                    }
                    catch { }

                    if (token.IsCancellationRequested) return;

                    var notifTag = GetNotificationTag();

                    try
                    {
                        ToastNotificationManagerCompat.History.Remove(notifTag);
                    }
                    catch { }

                    if (token.IsCancellationRequested) return;

                    new ToastContentBuilder()
                        .AddText(track.Name, AdaptiveTextStyle.Base, hintMaxLines: 1)
                        .AddText(track.Artist, AdaptiveTextStyle.Body, hintMaxLines: 1)
                        .AddText(track.Album, AdaptiveTextStyle.Body, hintMaxLines: 1)
                        .AddAppLogoOverride(_artworkUri)
                        .AddAudio(null, silent: true) // Disable sound
                        .Show(async (t) =>
                        {
                            t.ExpirationTime = DateTimeOffset.Now.AddSeconds(5);
                            t.Tag = notifTag;

                            try
                            {
                                await Task.Delay(5250, token);

                                ToastNotificationManagerCompat.History.Remove(t.Tag);
                            }
                            catch { }
                        });
                });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
                _ = ArtworkDispatcherCtrl.ShutdownQueueAsync();

                Destroy();
                _statusTimer?.Stop();
                _statusTimer?.Dispose();

                _currentTrack = null;
                _currentVolume = null;
            }

            base.Dispose(disposing);
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~AppleMusicController()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }
    }
}
