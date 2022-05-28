using iTunes.SMTC.Extensions;
using iTunes.SMTC.Model;
using iTunesLib;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Timer = System.Timers.Timer;

namespace iTunes.SMTC
{
    public partial class SettingsUi
    {
        private const string NOTIF_TAG = "iTunes.SMTC";

        private iTunesApp _iTunesApp;
        private TrackMetadata _currentTrack;
        private bool _isPlaying = false;

        private MediaPlayer _mediaPlayer;
        private SystemMediaTransportControls _systemMediaTransportControls;
        private bool _metadataEmpty = true;

        private DispatcherQueueController iTunesDispatcherCtrl;
        private DispatcherQueue iTunesDispatcher;

        private DispatcherQueueTimer _timelineTimer;
        private Timer _statusTimer;
        private Timer _delayStartTimer;

        private Uri _artworkUri;

        private CancellationTokenSource cts = null;

        private void InitializeSMTC()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.CommandManager.IsEnabled = false;
            _systemMediaTransportControls = _mediaPlayer.SystemMediaTransportControls;
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

        private void InitializeiTunesController()
        {
            _delayStartTimer = new Timer()
            {
                AutoReset = false,
                Interval = 35000
            };

            _timelineTimer = iTunesDispatcher.CreateTimer();
            _timelineTimer.Interval = TimeSpan.FromSeconds(1);
            _timelineTimer.Tick += (s, e) =>
            {
                UpdateSMTCTimeline(_currentTrack);

                if (!_isPlaying)
                {
                    s.Stop();
                }
            };
            _timelineTimer.IsRepeating = true;

            _statusTimer = new Timer()
            {
                AutoReset = true,
                Interval = 1000
            };
            _statusTimer.Elapsed += (s, e) =>
            {
                _statusTimer.Stop();

                try
                {
                    // Check if iTunes is currently running
                    if (IsiTunesRunning())
                    {
                        // If running, check connection status
                        if (_iTunesApp == null && !_delayStartTimer.Enabled)
                        {
                            InitializeiTunes();
                        }

                        // Update SMTC display
                        // NOTE: Needed as the iTunes OnPlayerPlayEvent does not always fire
                        iTunesDispatcher.TryEnqueue(() =>
                        {
                            try
                            {
                                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;
                                var currentTrack = _iTunesApp?.CurrentTrack?.GetMetadata();

                                var isPlaying = currentTrack != null && playerState == ITPlayerState.ITPlayerStatePlaying;

                                if (currentTrack == null)
                                {
                                    _isPlaying = false;
                                    // Only update if track is N/A and metadata is populated
                                    if (!_metadataEmpty)
                                    {
                                        UpdateSMTCDisplay(null);
                                    }
                                }
                                else if (_currentTrack == null || _currentTrack.DatabaseID != currentTrack?.DatabaseID)
                                {
                                    _isPlaying = isPlaying;
                                    SaveArtwork(currentTrack);
                                    UpdateSMTCDisplay(currentTrack);

                                    if (_isPlaying && Settings.ShowTrackToast)
                                    {
                                        ShowToastNotification(currentTrack);
                                    }
                                }
                                else if (_isPlaying != isPlaying)
                                {
                                    _isPlaying = isPlaying;
                                    UpdateSMTCPlaybackState(currentTrack);
                                }

                                _currentTrack = currentTrack;

                                if (_isPlaying && _timelineTimer?.IsRunning != true)
                                {
                                    _timelineTimer?.Start();
                                }
                            }
                            catch (Exception)
                            {
                                // no-op
                            }
                        });
                    }
                    else
                    {
                        // Stop timer once process has ended
                        _delayStartTimer?.Stop();

                        if (_iTunesApp != null)
                        {
                            DisconnectiTunes();
                        }
                    }

                    _statusTimer.Start();
                }
                catch (Exception)
                {
                    _statusTimer.Start();
                }
            };
            _statusTimer.Start();
        }

        private void InitializeiTunes()
        {
            Debug.WriteLine("Connecting to iTunes...");

            _iTunesApp = new iTunesAppClass();

            IntializeEvents();
            InitializeControls(_iTunesApp?.CurrentTrack?.GetMetadata());
        }

        private void SaveArtwork(TrackMetadata track)
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
            }

            try
            {
                if (track?.Artwork != null)
                {
                    // Save artwork to file
                    track.Artwork.SaveArtworkToFile(_artworkUri.LocalPath);
                    return;
                }

                // Delete artwork or replace with empty
                Properties.Resources.no_artwork.Save(_artworkUri.LocalPath);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }
        }

        private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        _iTunesApp?.PlayOrResume();
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        _iTunesApp?.PauseOrResume();
                        break;
                    case SystemMediaTransportControlsButton.Stop:
                        _iTunesApp?.Stop();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        _iTunesApp?.BackTrack();
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        _iTunesApp?.NextTrack();
                        break;
                    case SystemMediaTransportControlsButton.Rewind:
                        _iTunesApp?.Rewind();
                        break;
                    case SystemMediaTransportControlsButton.FastForward:
                        _iTunesApp?.FastForward();
                        break;
                }
            });
        }

        private void SystemControls_ShuffleEnabledChangeRequested(SystemMediaTransportControls sender, ShuffleEnabledChangeRequestedEventArgs args)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                if (_iTunesApp != null)
                {
                    var currentPlaylist = _iTunesApp.CurrentPlaylist;

                    if (currentPlaylist != null && _iTunesApp.CanSetShuffle[currentPlaylist])
                    {
                        currentPlaylist.Shuffle = args.RequestedShuffleEnabled;
                        sender.ShuffleEnabled = args.RequestedShuffleEnabled;
                    }
                    else
                    {
                        sender.ShuffleEnabled = false;
                    }
                }
            });
        }

        private void SystemControls_AutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                if (_iTunesApp != null)
                {
                    var currentPlaylist = _iTunesApp.CurrentPlaylist;

                    if (currentPlaylist != null && _iTunesApp.CanSetSongRepeat[currentPlaylist])
                    {
                        switch (args.RequestedAutoRepeatMode)
                        {
                            case MediaPlaybackAutoRepeatMode.None:
                                currentPlaylist.SongRepeat = ITPlaylistRepeatMode.ITPlaylistRepeatModeOff;
                                break;
                            case MediaPlaybackAutoRepeatMode.Track:
                                currentPlaylist.SongRepeat = ITPlaylistRepeatMode.ITPlaylistRepeatModeOne;
                                break;
                            case MediaPlaybackAutoRepeatMode.List:
                                currentPlaylist.SongRepeat = ITPlaylistRepeatMode.ITPlaylistRepeatModeAll;
                                break;
                        }

                        sender.AutoRepeatMode = args.RequestedAutoRepeatMode;
                    }
                    else
                    {
                        sender.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;
                    }
                }
            });
        }

        private void SystemControls_PlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                if (_iTunesApp != null)
                {
                    _iTunesApp.PlayerPosition = (int)args.RequestedPlaybackPosition.TotalSeconds;
                }
            });
        }

        private void IntializeEvents()
        {
            if (_iTunesApp != null)
            {
                _iTunesApp.OnPlayerPlayingTrackChangedEvent += _iTunesApp_OnPlayerPlayingTrackChangedEvent;
                _iTunesApp.OnPlayerPlayEvent += _iTunesApp_OnPlayerPlayEvent;
                _iTunesApp.OnPlayerStopEvent += _iTunesApp_OnPlayerStopEvent;
                _iTunesApp.OnQuittingEvent += _iTunesApp_OnQuittingEvent;
                _iTunesApp.OnAboutToPromptUserToQuitEvent += _iTunesApp_OnAboutToPromptUserToQuitEvent;
            }
        }

        private void RemoveEvents()
        {
            if (_iTunesApp != null)
            {
                try
                {
                    _iTunesApp.OnPlayerPlayingTrackChangedEvent -= _iTunesApp_OnPlayerPlayingTrackChangedEvent;
                    _iTunesApp.OnPlayerPlayEvent -= _iTunesApp_OnPlayerPlayEvent;
                    _iTunesApp.OnPlayerStopEvent -= _iTunesApp_OnPlayerStopEvent;
                    _iTunesApp.OnQuittingEvent -= _iTunesApp_OnQuittingEvent;
                    _iTunesApp.OnAboutToPromptUserToQuitEvent -= _iTunesApp_OnAboutToPromptUserToQuitEvent;
                }
                catch (Exception)
                {
                    // Ignore. iTunes may be in the process of ending
                }
            }
        }

        private void _iTunesApp_OnAboutToPromptUserToQuitEvent()
        {
            // Disconnect before user is prompted, to avoid 'scripting' popup
            _statusTimer?.Stop();
            DisconnectiTunes();
        }

        private void _iTunesApp_OnQuittingEvent()
        {
            _statusTimer?.Stop();
            DisconnectiTunes();
        }

        private void DisconnectiTunes()
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                Debug.WriteLine("Disconnecting...");

                _statusTimer?.Stop();
                _timelineTimer?.Stop();

                var wasAlive = false;

                if (_currentTrack != null)
                {
                    Marshal.FinalReleaseComObject(_currentTrack);
                    GC.Collect();
                }
                _currentTrack = null;

                if (_iTunesApp != null)
                {
                    Debug.WriteLine("Releasing iTunes COM object...");
                    wasAlive = true;

                    RemoveEvents();
                    Marshal.FinalReleaseComObject(_iTunesApp);
                    GC.Collect();
                }
                _iTunesApp = null;

                _isPlaying = false;

                if (_systemMediaTransportControls != null)
                {
                    _systemMediaTransportControls.IsEnabled = false;
                }

                // Give process time to end
                if (wasAlive)
                {
                    Debug.WriteLine("Starting disconnect delay...");
                    System.Threading.Thread.Sleep(10000); // 10s
                    Debug.WriteLine("Ended disconnect delay...");

                    // NOTE: if the scripting popup appears, iTunes will close in 30s
                    // Therefore the process may still be alive after disconnecting
                    // To prevent restarting iTunes after disconnecting (since the process is still alive, delay the status timer
                    // from starting by waiting ~30s; We'll stop timer once time has expired or iTunes process ended
                    _delayStartTimer?.Start();
                }

                _statusTimer?.Start();
            });
        }

        private void InitializeControls(TrackMetadata currentTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();

                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

                _isPlaying = currentTrack != null && playerState == ITPlayerState.ITPlayerStatePlaying;

                SaveArtwork(currentTrack);
                UpdateSMTCDisplay(currentTrack);
                UpdateSMTCTimeline(currentTrack);

                _currentTrack = currentTrack;

                _statusTimer?.Start();

                if (_isPlaying)
                {
                    _timelineTimer?.Start();
                }
            });
        }

        private void _iTunesApp_OnPlayerStopEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();
                _timelineTimer?.Stop();

                var isPlaying = false;

                var track = GetCurrentTrack(iTrack)?.GetMetadata();

                if (_currentTrack == null || _currentTrack.DatabaseID != track?.DatabaseID)
                {
                    _isPlaying = isPlaying;
                    SaveArtwork(track);
                    UpdateSMTCDisplay(track);
                }
                else if (_isPlaying != isPlaying)
                {
                    _isPlaying = isPlaying;
                    UpdateSMTCPlaybackState(track);
                }

                _currentTrack = track;

                _statusTimer?.Start();
            });
        }

        private void _iTunesApp_OnPlayerPlayEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();
                _timelineTimer?.Stop();

                var isPlaying = true;

                var track = GetCurrentTrack(iTrack)?.GetMetadata();

                if (_currentTrack == null || _currentTrack.DatabaseID != track?.DatabaseID)
                {
                    _isPlaying = isPlaying;
                    SaveArtwork(track);
                    UpdateSMTCDisplay(track);
                }
                else if (_isPlaying != isPlaying)
                {
                    _isPlaying = isPlaying;
                    UpdateSMTCPlaybackState(track);
                }

                if (Settings.ShowTrackToast)
                {
                    ShowToastNotification(track);
                }

                _currentTrack = track;

                _statusTimer?.Start();
                _timelineTimer?.Start();
            });
        }

        private void _iTunesApp_OnPlayerPlayingTrackChangedEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();

                var currentTrack = GetCurrentTrack(iTrack)?.GetMetadata();
                UpdateSMTCDisplay(currentTrack);
                _currentTrack = currentTrack;

                _statusTimer?.Start();
            });
        }

        private IITTrack GetCurrentTrack(object iTrack)
        {
            IITTrack currentTrack;

            if (iTrack is IITTrack track)
            {
                currentTrack = track;
            }
            else
            {
                currentTrack = _iTunesApp?.CurrentTrack;
            }

            return currentTrack;
        }

        private void UpdateSMTCDisplay(TrackMetadata currentTrack)
        {
            RunOnUIThread(async () =>
            {
                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

                switch (playerState)
                {
                    case ITPlayerState.ITPlayerStateStopped:
                        _systemMediaTransportControls.PlaybackStatus = currentTrack != null ? MediaPlaybackStatus.Paused : MediaPlaybackStatus.Stopped;
                        break;
                    case ITPlayerState.ITPlayerStatePlaying:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                        break;
                    case ITPlayerState.ITPlayerStateFastForward:
                    case ITPlayerState.ITPlayerStateRewind:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                        break;
                }

                _systemMediaTransportControls.IsEnabled = currentTrack != null;

                _systemMediaTransportControls.ShuffleEnabled = _iTunesApp?.CurrentPlaylist?.Shuffle ?? false;
                _systemMediaTransportControls.AutoRepeatMode = GetRepeatModeFromITunes(_iTunesApp?.CurrentPlaylist?.SongRepeat) ?? MediaPlaybackAutoRepeatMode.None;

                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.ClearAll();

                if (currentTrack != null)
                {
                    updater.Type = MediaPlaybackType.Music;
                    updater.AppMediaId = currentTrack?.DatabaseID.ToString();
                    updater.MusicProperties.Artist = currentTrack?.Artist;
                    updater.MusicProperties.AlbumTitle = currentTrack?.Album;
                    updater.MusicProperties.Title = currentTrack?.Name;
                    updater.MusicProperties.TrackNumber = currentTrack?.TrackNumber ?? 0;

                    try
                    {
                        updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));
                    }
                    catch (Exception ex)
                    {
                        Crashes.TrackError(ex);
                    }

                    _metadataEmpty = false;
                }
                else
                {
                    _metadataEmpty = true;
                }

                updater.Update();
            });
        }

        private void UpdateSMTCPlaybackState(TrackMetadata currentTrack)
        {
            RunOnUIThread(() =>
            {
                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

                switch (playerState)
                {
                    case ITPlayerState.ITPlayerStateStopped:
                        _systemMediaTransportControls.PlaybackStatus = currentTrack != null ? MediaPlaybackStatus.Paused : MediaPlaybackStatus.Stopped;
                        break;
                    case ITPlayerState.ITPlayerStatePlaying:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                        break;
                    case ITPlayerState.ITPlayerStateFastForward:
                    case ITPlayerState.ITPlayerStateRewind:
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                        break;
                }
            });
        }

        private void UpdateSMTCTimeline(TrackMetadata currentTrack)
        {
            RunOnUIThread(() =>
            {
                // Update timeline
                var timelineProperties = new SystemMediaTransportControlsTimelineProperties();

                if (currentTrack != null)
                {
                    timelineProperties.StartTime = TimeSpan.FromSeconds(currentTrack.StartTime);
                    timelineProperties.EndTime = TimeSpan.FromSeconds(currentTrack.EndTime);
                    timelineProperties.Position = TimeSpan.FromSeconds(_iTunesApp?.PlayerPosition ?? 0);

                    timelineProperties.MinSeekTime = TimeSpan.FromSeconds(currentTrack.StartTime);
                    timelineProperties.MaxSeekTime = TimeSpan.FromSeconds(currentTrack.EndTime);
                }

                _systemMediaTransportControls.UpdateTimelineProperties(timelineProperties);
            });
        }

        private static MediaPlaybackAutoRepeatMode? GetRepeatModeFromITunes(ITPlaylistRepeatMode? repeatMode)
        {
            return repeatMode switch
            {
                ITPlaylistRepeatMode.ITPlaylistRepeatModeOff => MediaPlaybackAutoRepeatMode.None,
                ITPlaylistRepeatMode.ITPlaylistRepeatModeOne => MediaPlaybackAutoRepeatMode.Track,
                ITPlaylistRepeatMode.ITPlaylistRepeatModeAll => MediaPlaybackAutoRepeatMode.List,
                _ => null,
            };
        }

        private static bool IsiTunesRunning()
        {
            Process[] iTunesProcesses = Process.GetProcessesByName("iTunes");

            return iTunesProcesses.Length > 0;
        }

        private void ResetToken()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
        }

        private void ShowToastNotification(TrackMetadata track)
        {
            if (track != null)
            {
                ResetToken();

                RunOnUIThread(() =>
                {
                    try
                    {
                        ToastNotificationManagerCompat.History.Remove(NOTIF_TAG);
                    }
                    catch { }

                    new ToastContentBuilder()
                        .AddText(track.Name, AdaptiveTextStyle.Base, hintMaxLines: 1)
                        .AddText(track.Artist, AdaptiveTextStyle.Body, hintMaxLines: 1)
                        .AddText(track.Album, AdaptiveTextStyle.Body, hintMaxLines: 1)
                        .AddAppLogoOverride(_artworkUri)
                        .AddAudio(null, silent: true) // Disable sound
                        .Show(async (t) =>
                        {
                            t.ExpirationTime = DateTimeOffset.Now.AddSeconds(5);
                            t.Tag = NOTIF_TAG;

                            try
                            {
                                await Task.Delay(5250, cts.Token);

                                ToastNotificationManagerCompat.History.Remove(t.Tag);
                            }
                            catch { }
                        });
                });
            }
        }
    }
}
