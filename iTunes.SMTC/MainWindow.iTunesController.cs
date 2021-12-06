using iTunesLib;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace iTunes.SMTC
{
    public sealed partial class MainWindow : Window
    {
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

        private StorageFolder ArtworkFolder;
        private StorageFile ArtworkFile;

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
            _systemMediaTransportControls.ButtonPressed += SystemControls_ButtonPressed;
        }

        private void InitializeiTunesController()
        {
            iTunesDispatcher.TryEnqueue(async () =>
            {
                // Create Artwork folder
                await GetArtworkFolder();
            });

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
                        if (_iTunesApp == null)
                        {
                            if (_delayStartTimer?.Enabled != true)
                            {
                                InitializeiTunes();
                            }
                        }

                        // Update SMTC display
                        // NOTE: Needed as the iTunes OnPlayerPlayEvent does not always fire
                        iTunesDispatcher.TryEnqueue(async () =>
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
                                    await SaveArtwork(currentTrack);
                                    UpdateSMTCDisplay(currentTrack);
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

        private async Task<StorageFolder> GetArtworkFolder()
        {
            if (ArtworkFolder == null)
            {
                ArtworkFolder = await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("Artwork", CreationCollisionOption.OpenIfExists);
            }

            return ArtworkFolder;
        }

        private async Task<StorageFile> GetDefaultArtworkFile()
        {
            if (ArtworkFile == null)
            {
                var folder = await GetArtworkFolder();
                ArtworkFile = await folder.CreateFileAsync("artwork.img", CreationCollisionOption.OpenIfExists);
            }

            return ArtworkFile;
        }

        private static async Task ClearArtworkFile(StorageFile file)
        {
            try
            {
                await FileIO.WriteBytesAsync(file, Array.Empty<byte>());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async Task SaveArtwork(IITTrack track)
        {
            var artworkFile = await GetDefaultArtworkFile();

            if (track != null)
            {
                try
                {
                    var artworks = track.Artwork.Cast<IITArtwork>();
                    var artwork = artworks.FirstOrDefault();
                    if (artwork != null)
                    {
                        artwork.SaveArtworkToFile(artworkFile.Path);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
            else
            {
                await ClearArtworkFile(artworkFile);
            }
        }

        private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        _iTunesApp?.Play();
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        _iTunesApp?.Pause();
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
                    System.Threading.Thread.Sleep(5000); // 5s
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
            iTunesDispatcher.TryEnqueue(async () =>
            {
                _statusTimer?.Stop();

                var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

                _isPlaying = currentTrack != null && playerState == ITPlayerState.ITPlayerStatePlaying;

                await SaveArtwork(currentTrack);
                UpdateSMTCDisplay(currentTrack);

                _currentTrack = currentTrack;

                _statusTimer?.Start();
            });
        }

        private void _iTunesApp_OnPlayerStopEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(async () =>
            {
                _statusTimer?.Stop();

                var isPlaying = false;

                var track = GetCurrentTrack(iTrack);

                if (_currentTrack == null || _currentTrack.TrackDatabaseID != track?.TrackDatabaseID)
                {
                    _isPlaying = isPlaying;
                    await SaveArtwork(track);
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
            iTunesDispatcher.TryEnqueue(async () =>
            {
                _statusTimer?.Stop();

                var isPlaying = true;

                var track = GetCurrentTrack(iTrack);

                if (_currentTrack == null || _currentTrack.TrackDatabaseID != track?.TrackDatabaseID)
                {
                    _isPlaying = isPlaying;
                    await SaveArtwork(track);
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

            DispatcherQueue.TryEnqueue(async () =>
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

                    updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await GetDefaultArtworkFile());

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

            DispatcherQueue.TryEnqueue(() =>
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

        private bool IsiTunesRunning()
        {
            Process[] iTunesProcesses = Process.GetProcessesByName("iTunes");

            return iTunesProcesses.Length > 0;
        }
    }
}
