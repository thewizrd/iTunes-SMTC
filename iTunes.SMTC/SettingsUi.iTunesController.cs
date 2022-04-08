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
        private IITTrack _currentTrack;
        private bool _isPlaying = false;

        private MediaPlayer _mediaPlayer;
        private SystemMediaTransportControls _systemMediaTransportControls;
        private bool _metadataEmpty = true;

        private DispatcherQueueController iTunesDispatcherCtrl;
        private DispatcherQueue iTunesDispatcher;

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
            _systemMediaTransportControls.ButtonPressed += SystemControls_ButtonPressed;
        }

        private void InitializeiTunesController()
        {
            _delayStartTimer = new Timer()
            {
                AutoReset = false,
                Interval = 35000
            };

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
                                var currentTrack = _iTunesApp?.CurrentTrack;

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
                                else if (_currentTrack == null || _currentTrack.TrackDatabaseID != currentTrack?.TrackDatabaseID)
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
                catch (Exception ex)
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
            InitializeControls(_iTunesApp?.CurrentTrack);
        }

        private void SaveArtwork(IITTrack track)
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

            if (track != null)
            {
                try
                {
                    var artworks = track.Artwork.Cast<IITArtwork>();
                    var artwork = artworks.FirstOrDefault();
                    if (artwork != null)
                    {
                        // Save artwork to file
                        artwork.SaveArtworkToFile(_artworkUri.LocalPath);
                    }
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                }
            }
        }

        private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;
                
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        if (playerState == ITPlayerState.ITPlayerStateRewind || playerState == ITPlayerState.ITPlayerStateFastForward)
                        {
                            _iTunesApp?.Resume();
                        }
                        else
                        {
                            _iTunesApp?.Play();
                        }
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        if (playerState == ITPlayerState.ITPlayerStateRewind || playerState == ITPlayerState.ITPlayerStateFastForward)
                        {
                            _iTunesApp?.Resume();
                        }
                        else
                        {
                            _iTunesApp?.Pause();
                        }
                        break;
                    case SystemMediaTransportControlsButton.Stop:
                        _iTunesApp?.Stop();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        _iTunesApp?.PreviousTrack();
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

                var wasAlive = false;

                if (_iTunesApp != null)
                {
                    Debug.WriteLine("Releasing iTunes COM object...");
                    wasAlive = true;

                    RemoveEvents();
                    Marshal.ReleaseComObject(_iTunesApp);
                }
                _iTunesApp = null;

                if (_currentTrack != null)
                {
                    Marshal.ReleaseComObject(_currentTrack);
                }
                _currentTrack = null;

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

        private void InitializeControls(IITTrack currentTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();

                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

                _isPlaying = currentTrack != null && playerState == ITPlayerState.ITPlayerStatePlaying;

                SaveArtwork(currentTrack);
                UpdateSMTCDisplay(currentTrack);

                _currentTrack = currentTrack;

                _statusTimer?.Start();
            });
        }

        private void _iTunesApp_OnPlayerStopEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();

                var isPlaying = false;

                var track = GetCurrentTrack(iTrack);

                if (_currentTrack == null || _currentTrack.TrackDatabaseID != track?.TrackDatabaseID)
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

                var isPlaying = true;

                var track = GetCurrentTrack(iTrack);

                if (_currentTrack == null || _currentTrack.TrackDatabaseID != track?.TrackDatabaseID)
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
            });
        }

        private void _iTunesApp_OnPlayerPlayingTrackChangedEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                _statusTimer?.Stop();

                var currentTrack = GetCurrentTrack(iTrack);
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

        private void UpdateSMTCDisplay(IITTrack currentTrack)
        {
            var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

            RunOnUIThread(async () =>
            {
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
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                        break;
                }

                _systemMediaTransportControls.IsEnabled = currentTrack != null;

                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.ClearAll();

                if (currentTrack != null)
                {
                    updater.Type = MediaPlaybackType.Music;
                    updater.MusicProperties.Artist = currentTrack?.Artist;
                    updater.MusicProperties.AlbumTitle = currentTrack?.Album;
                    updater.MusicProperties.Title = currentTrack?.Name;

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

        private void UpdateSMTCPlaybackState(IITTrack currentTrack)
        {
            var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

            RunOnUIThread(() =>
            {
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
                        _systemMediaTransportControls.PlaybackStatus = MediaPlaybackStatus.Changing;
                        break;
                }
            });
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

        private void ShowToastNotification(IITTrack track)
        {
            if (track != null)
            {
                ResetToken();

                RunOnUIThread(() =>
                {
                    ToastNotificationManagerCompat.History.Remove(NOTIF_TAG);

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
                            }
                            catch (Exception) { }

                            ToastNotificationManagerCompat.History.Remove(t.Tag);
                        });
                });
            }
        }
    }
}
