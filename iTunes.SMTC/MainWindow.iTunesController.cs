using iTunesLib;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private DispatcherQueueController iTunesDispatcherCtrl;
        private DispatcherQueue iTunesDispatcher;

        private StorageFolder ArtworkFolder;
        private StorageFile ArtworkFile;

        private void InitializeSMTC()
        {
            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.CommandManager.IsEnabled = false;
            _systemMediaTransportControls = _mediaPlayer.SystemMediaTransportControls;
            _systemMediaTransportControls.IsEnabled = true;
            _systemMediaTransportControls.IsNextEnabled = true;
            _systemMediaTransportControls.IsPauseEnabled = true;
            _systemMediaTransportControls.IsPlayEnabled = true;
            _systemMediaTransportControls.IsPreviousEnabled = true;
            _systemMediaTransportControls.IsStopEnabled = true;
            _systemMediaTransportControls.ButtonPressed += SystemControls_ButtonPressed;
        }

        private void InitializeITunes()
        {
            _iTunesApp = new iTunesAppClass();
            iTunesDispatcher.TryEnqueue(async () =>
            {
                // Create Artwork folder
                await GetArtworkFolder();
            });
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
            await FileIO.WriteBytesAsync(file, Array.Empty<byte>());
        }

        private async Task SaveArtwork(IITTrack track)
        {
            var artworkFile = await GetDefaultArtworkFile();

            if (track != null)
            {
                var artworks = track.Artwork.Cast<IITArtwork>();
                var artwork = artworks.FirstOrDefault();
                if (artwork != null)
                {
                    artwork.SaveArtworkToFile(artworkFile.Path);
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
                        _iTunesApp.Play();
                        sender.PlaybackStatus = MediaPlaybackStatus.Playing;
                        break;
                    case SystemMediaTransportControlsButton.Pause:
                        _iTunesApp.Pause();
                        sender.PlaybackStatus = MediaPlaybackStatus.Paused;
                        break;
                    case SystemMediaTransportControlsButton.Stop:
                        _iTunesApp.Stop();
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        _iTunesApp.PreviousTrack();
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        _iTunesApp.NextTrack();
                        break;
                    case SystemMediaTransportControlsButton.Rewind:
                        _iTunesApp.Rewind();
                        break;
                    case SystemMediaTransportControlsButton.FastForward:
                        _iTunesApp.FastForward();
                        break;
                }
            });
        }

        private void IntializeEvents()
        {
            _iTunesApp.OnPlayerPlayingTrackChangedEvent += _iTunesApp_OnPlayerPlayingTrackChangedEvent;
            _iTunesApp.OnPlayerPlayEvent += _iTunesApp_OnPlayerPlayEvent;
            _iTunesApp.OnPlayerStopEvent += _iTunesApp_OnPlayerStopEvent;
            _iTunesApp.OnQuittingEvent += _iTunesApp_OnQuittingEvent;
        }

        private void _iTunesApp_OnQuittingEvent()
        {
            // no-op
        }

        private void InitializeControls(IITTrack currentTrack)
        {
            iTunesDispatcher.TryEnqueue(async () =>
            {
                _isPlaying = currentTrack != null && _iTunesApp.PlayerState == ITPlayerState.ITPlayerStatePlaying;

                await SaveArtwork(currentTrack);
                UpdateSMTCDisplay(currentTrack);

                _currentTrack = currentTrack;
            });
        }

        private void _iTunesApp_OnPlayerStopEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(async () =>
            {
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
            });
        }

        private void _iTunesApp_OnPlayerPlayEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(async () =>
            {
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
            });
        }

        private void _iTunesApp_OnPlayerPlayingTrackChangedEvent(object iTrack)
        {
            iTunesDispatcher.TryEnqueue(() =>
            {
                var currentTrack = GetCurrentTrack(iTrack);
                UpdateSMTCDisplay(currentTrack);
                _currentTrack = currentTrack;
            });
        }

        private IITTrack GetCurrentTrack(object iTrack)
        {
            ThrowIfDisposed();

            IITTrack currentTrack = null;

            if (iTrack is IITTrack)
            {
                currentTrack = (IITTrack)iTrack;
            }
            else
            {
                currentTrack = _iTunesApp.CurrentTrack;
            }

            return currentTrack;
        }

        private void UpdateSMTCDisplay(IITTrack currentTrack)
        {
            ThrowIfDisposed();
            var playerState = _iTunesApp.PlayerState;

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

                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.ClearAll();

                updater.Type = MediaPlaybackType.Music;
                if (currentTrack != null)
                {
                    updater.MusicProperties.Artist = currentTrack?.Artist;
                    updater.MusicProperties.AlbumTitle = currentTrack?.Album;
                    updater.MusicProperties.Title = currentTrack?.Name;

                    updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await GetDefaultArtworkFile());
                }
                else
                {
                    updater.MusicProperties.Title = "iTunes - SMTC";
                }

                updater.Update();
            });
        }

        private void UpdateSMTCPlaybackState(IITTrack currentTrack)
        {
            ThrowIfDisposed();
            var playerState = _iTunesApp.PlayerState;

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
    }
}
