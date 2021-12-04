using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using Windows.Foundation;
using Windows.Foundation.Collections;

using iTunesLib;
using Windows.Media;
using Microsoft.UI.Dispatching;
using Windows.Media.Playback;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace iTunes_SMTC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, IDisposable
    {
        private iTunesApp _iTunesApp;
        private IITTrack _currentTrack;
        private bool _isPlaying = false;

        private SystemMediaTransportControls _systemMediaTransportControls;

        private DispatcherQueueController iTunesDispatcherCtrl;
        private DispatcherQueue iTunesDispatcher;
        private bool disposedValue;

        private StorageFolder ArtworkFolder;
        private StorageFile ArtworkFile;

        public MainWindow()
        {
            InitializeComponent();

            iTunesDispatcherCtrl = DispatcherQueueController.CreateOnDedicatedThread();
            iTunesDispatcher = iTunesDispatcherCtrl.DispatcherQueue;

            InitializeITunes();
            InitializeSMTC();
            IntializeEvents();

            InitializeControls(_iTunesApp.CurrentTrack);
        }

        private void InitializeSMTC()
        {
            _systemMediaTransportControls = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
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
            throw new NotImplementedException();
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

        private void ThrowIfDisposed()
        {
            if (disposedValue)
            {
                throw new ObjectDisposedException(typeof(iTunesApp).FullName);
            }
        }
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                if (_iTunesApp != null)
                {
                    Marshal.ReleaseComObject(_iTunesApp);
                }
                // TODO: set large fields to null
                _iTunesApp = null;
                disposedValue = true;
            }
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~MainWindow()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
