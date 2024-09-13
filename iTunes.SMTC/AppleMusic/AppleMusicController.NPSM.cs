using iTunes.SMTC.AppleMusic.Model;
using iTunes.SMTC.Utils;
using Microsoft.AppCenter.Crashes;
using NPSMLib;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController
    {
        private NowPlayingSessionManager NPSManager;
        private NowPlayingSession MediaSession;
        private MediaPlaybackDataSource MediaPlaybackSource;
        private NPSMInfo _npsmInfo;

        private bool UseMediaSession = false;

        private void StartNPSMService()
        {
            if (NPSManager != null)
            {
                StopNPSMService();
            }

            try
            {
                NPSManager = new NowPlayingSessionManager();
                NPSManager.SessionListChanged += NPSManager_SessionsChanged;
                ReloadSessions(NPSManager);
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
                // Fallback
                _statusTimer?.Start();
            }
        }

        private void NPSManager_SessionsChanged(object sender, NowPlayingSessionManagerEventArgs args)
        {
            if (args.NotificationType == NowPlayingSessionManagerNotificationType.SessionCreated)
            {
                // No sessions available; reload else ignore
                if (MediaSession == null)
                {
                    ReloadSessions(sender as NowPlayingSessionManager ?? NPSManager);
                }
            }
            else if (args.NotificationType == NowPlayingSessionManagerNotificationType.CurrentSessionChanged)
            {
                // ignore
            }
            else if (args.NotificationType == NowPlayingSessionManagerNotificationType.SessionDisconnected)
            {
                if (MediaSession != null && Equals(MediaSession.GetSessionInfo(), args.NowPlayingSessionInfo))
                {
                    ReloadSessions(sender as NowPlayingSessionManager ?? NPSManager);
                }
            }
        }

        private void ReloadSessions(NowPlayingSessionManager sessionManager)
        {
            // If a session is available disable FlaUI fallback
            UnloadSession();
            MediaSession = sessionManager?.GetSessions()?.FirstOrDefault(s => s.SourceAppId?.Contains("AppleMusic") == true);

            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Interval = MediaSession != null ? 5000 : 1000;
                _statusTimer.Start();
            }

            LoadSession();
        }

        private void LoadSession()
        {
            if (MediaSession != null)
            {
                MediaPlaybackSource = MediaSession.ActivateMediaPlaybackDataSource();
                MediaPlaybackSource.MediaPlaybackDataChanged += MediaPlaybackSource_MediaPlaybackDataChanged;

                _npsmInfo = new NPSMInfo();

                UpdatePlayer(MediaPlaybackSource);

                if (TrackChanged?.HasListeners() == true)
                {
                    AMDispatcher.TryEnqueue(() =>
                    {
                        if (UseMediaSession && _npsmInfo != null)
                        {
                            TrackChanged?.Invoke(this, _npsmInfo.ToPlayerStateModel(true));
                        }
                    });
                }
            }
        }

        private void StopNPSMService()
        {
            // Unregister events
            if (NPSManager != null)
            {
                try
                {
                    NPSManager.SessionListChanged -= NPSManager_SessionsChanged;
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                }
            }

            NPSManager = null;
            MediaSession = null;
            _npsmInfo = null;
        }

        private void UnloadSession()
        {
            if (MediaPlaybackSource != null)
            {
                try
                {
                    MediaPlaybackSource.MediaPlaybackDataChanged -= MediaPlaybackSource_MediaPlaybackDataChanged;
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                }
            }

            MediaPlaybackSource = null;
            MediaSession = null;
            _npsmInfo = null;
        }

        private void UpdatePlayer(MediaPlaybackDataSource source)
        {
            UpdateMediaProperties(source);
            UpdateTimeline(source);
            UpdatePlaybackInfo(source);
        }

        private void UpdateMediaProperties(MediaPlaybackDataSource source)
        {
            var mediaObjectInfo = source.GetMediaObjectInfo();
            var thumbnailStream = source.GetThumbnailStream();

            AMDispatcher.TryEnqueue(() =>
            {
                if (_npsmInfo != null)
                {
                    _npsmInfo.TrackData.Name = mediaObjectInfo.Title;
                    _npsmInfo.TrackData.Artist = mediaObjectInfo.Artist;
                    _npsmInfo.TrackData.Album = mediaObjectInfo.AlbumTitle;

                    if (string.IsNullOrWhiteSpace(_npsmInfo.TrackData.Artist) && !string.IsNullOrWhiteSpace(mediaObjectInfo.AlbumArtist))
                    {
                        _npsmInfo.TrackData.Artist = mediaObjectInfo.AlbumArtist;
                    }

                    // Artist and Album name are sent together separated by " — " character
                    // Split the two to get the names separately
                    if (_npsmInfo.TrackData.Artist?.Contains(" — ") == true)
                    {
                        var artistAlbumInfos = _npsmInfo.TrackData.Artist.Split(" — ");

                        // Note: If more than 2 than last part is likely station name
                        if (artistAlbumInfos.Length >= 2)
                        {
                            _npsmInfo.TrackData.Artist = artistAlbumInfos[0];
                            _npsmInfo.TrackData.Album = artistAlbumInfos[1];
                        }
                    }
                }

                SystemMediaTransportControlsDisplayUpdater updater = _systemMediaTransportControls.DisplayUpdater;
                updater.ClearAll();

                if (_npsmInfo != null)
                {
                    updater.Type = MediaPlaybackType.Music;
                    updater.MusicProperties.Title = _npsmInfo?.TrackData?.Name;
                    updater.MusicProperties.Artist = _npsmInfo?.TrackData?.Artist;
                    updater.MusicProperties.AlbumTitle = _npsmInfo?.TrackData?.Album;

                    if (thumbnailStream != null)
                    {
                        ResetArtworkToken();
                        var token = artworkCts.Token;

                        ArtworkDispatcher.TryEnqueue(async () =>
                        {
                            await Task.Delay(500);

                            if (token.IsCancellationRequested) return;

                            await SaveArtwork(thumbnailStream);

                            try
                            {
                                if (token.IsCancellationRequested) return;

                                updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));

                                if (_npsmInfo.TrackData != null)
                                {
                                    _npsmInfo.TrackData.Artwork = await updater.Thumbnail.ToBytes();
                                }

                                updater.Update();

                                if (token.IsCancellationRequested) return;

                                if (ArtworkChanged?.HasListeners() == true)
                                {
                                    ArtworkChanged?.Invoke(this, new ArtworkModel { Artwork = (_npsmInfo?.TrackData?.Artwork ?? await updater.Thumbnail.ToBytes()) });
                                }
                            }
                            catch (Exception ex)
                            {
                                Crashes.TrackError(ex);
                            }
                        });
                    }

                    _currentTrack = _npsmInfo?.TrackData;
                    _metadataEmpty = false;
                    UseMediaSession = (!_npsmInfo?.TrackData?.IsEmpty) ?? false;
                }
                else
                {
                    _currentTrack = null;
                    _isPlaying = false;
                    _metadataEmpty = true;
                    UseMediaSession = false;
                }

                _systemMediaTransportControls.IsEnabled = !_metadataEmpty;
                updater.Update();
            });
        }

        private void UpdateTimeline(MediaPlaybackDataSource source)
        {
            var timelineProperties = source.GetMediaTimelineProperties();

            AMDispatcher.TryEnqueue(() =>
            {
                _systemMediaTransportControls.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties()
                {
                    StartTime = timelineProperties.StartTime,
                    EndTime = timelineProperties.EndTime,
                    Position = timelineProperties.Position,
                    MaxSeekTime = timelineProperties.MaxSeekTime,
                    MinSeekTime = timelineProperties.MinSeekTime
                });

                if (_npsmInfo != null)
                {
                    _npsmInfo.TrackProgress = (int)timelineProperties.Position.TotalSeconds;

                    if (_npsmInfo.TrackData != null)
                    {
                        _npsmInfo.TrackData.Duration = (int)timelineProperties.EndTime.TotalSeconds;
                    }
                }
            });
        }

        private void UpdatePlaybackInfo(MediaPlaybackDataSource source)
        {
            var playbackInfo = source.GetMediaPlaybackInfo();

            AMDispatcher.TryEnqueue(() =>
            {
                var playerCapabilities = playbackInfo.PlaybackCaps;
                var playerValidProps = playbackInfo.PropsValid;

                if (_npsmInfo != null)
                {
                    _isPlaying = _npsmInfo.IsPlaying = (playerValidProps.HasFlag(MediaPlaybackProps.State) ? playbackInfo.PlaybackState : MediaPlaybackState.Unknown) switch
                    {
                        MediaPlaybackState.Playing => true,
                        _ => false,
                    };

                    _systemMediaTransportControls.IsPreviousEnabled = _npsmInfo.IsPreviousEnabled = playerCapabilities.HasFlag(MediaPlaybackCapabilities.Previous);
                    _systemMediaTransportControls.IsNextEnabled = _npsmInfo.IsNextEnabled = playerCapabilities.HasFlag(MediaPlaybackCapabilities.Next);
                    _systemMediaTransportControls.IsPauseEnabled = _npsmInfo.IsPauseEnabled = playerCapabilities.HasFlag(MediaPlaybackCapabilities.PlayPauseToggle);
                    _systemMediaTransportControls.IsPlayEnabled = _npsmInfo.IsPlayEnabled = playerCapabilities.HasFlag(MediaPlaybackCapabilities.Play);
                    _systemMediaTransportControls.IsStopEnabled = _npsmInfo.IsStopEnabled = playerCapabilities.HasFlag(MediaPlaybackCapabilities.Stop);

                    if (playerValidProps.HasFlag(MediaPlaybackProps.State))
                    {
                        _systemMediaTransportControls.PlaybackStatus = playbackInfo.PlaybackState switch
                        {
                            MediaPlaybackState.Closed => MediaPlaybackStatus.Closed,
                            MediaPlaybackState.Opened => MediaPlaybackStatus.Paused,
                            MediaPlaybackState.Changing => MediaPlaybackStatus.Changing,
                            MediaPlaybackState.Playing => MediaPlaybackStatus.Playing,
                            MediaPlaybackState.Paused => MediaPlaybackStatus.Paused,
                            _ => MediaPlaybackStatus.Stopped,
                        };
                    }

                    _npsmInfo.VolumeState = _currentVolume;
                }
            });
        }

        private void MediaPlaybackSource_MediaPlaybackDataChanged(object sender, MediaPlaybackDataChangedArgs e)
        {
            switch (e.DataChangedEvent)
            {
                case MediaPlaybackDataChangedEvent.PlaybackInfoChanged:
                    {
                        var wasPlaying = _isPlaying;
                        UpdatePlaybackInfo(e.MediaPlaybackDataSource);

                        // Queue up notification
                        AMDispatcher.TryEnqueue(() =>
                        {
                            if (!wasPlaying && _isPlaying)
                            {
                                if (Settings.ShowTrackToast)
                                {
                                    ShowToastNotification(_currentTrack);
                                }
                            }

                            if (((!wasPlaying && _isPlaying) || (!_isPlaying && wasPlaying)) && PlayerStateChanged?.HasListeners() == true)
                            {
                                if (!UseMediaSession)
                                {
                                    PlayerStateChanged?.Invoke(this, GetAMPlayerInfo().ToPlayerStateModel(true));
                                }
                                else
                                {
                                    PlayerStateChanged?.Invoke(this, _npsmInfo.ToPlayerStateModel(true));
                                }
                            }
                        });
                    }
                    break;
                case MediaPlaybackDataChangedEvent.MediaInfoChanged:
                    {
                        var prevTrack = _currentTrack?.Copy();
                        UpdateMediaProperties(e.MediaPlaybackDataSource);

                        // Queue up notification
                        AMDispatcher.TryEnqueue(() =>
                        {
                            if ((prevTrack == null || !Equals(prevTrack, _currentTrack)))
                            {
                                if (Settings.ShowTrackToast)
                                {
                                    ShowToastNotification(_currentTrack);
                                }

                                // Skip empty track events
                                if (!(_isPlaying && _currentTrack?.IsEmpty == true) && TrackChanged?.HasListeners() == true)
                                {
                                    TrackChanged?.Invoke(this, _npsmInfo.ToPlayerStateModel(true));
                                }
                            }
                            else if (prevTrack != null && _currentTrack != null && prevTrack.Artwork?.AsSpan().SequenceEqual(_currentTrack.Artwork) != true)
                            {
                                ResetArtworkToken();
                                var token = artworkCts.Token;

                                // Check for artwork change
                                ArtworkDispatcher.TryEnqueue(async () =>
                                {
                                    await Task.Delay(500);

                                    if (token.IsCancellationRequested) return;

                                    var updater = _systemMediaTransportControls.DisplayUpdater;
                                    updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));
                                    updater.Update();

                                    if (token.IsCancellationRequested) return;

                                    if (ArtworkChanged?.HasListeners() == true)
                                    {
                                        ArtworkChanged?.Invoke(this, new ArtworkModel { Artwork = await updater.Thumbnail.ToBytes() });
                                    }
                                });
                            }
                        });
                    }
                    break;
                case MediaPlaybackDataChangedEvent.TimelinePropertiesChanged:
                    {
                        var prevTrack = _currentTrack?.Copy();
                        UpdateTimeline(e.MediaPlaybackDataSource);

                        // Queue up notification
                        AMDispatcher.TryEnqueue(() =>
                        {
                            if (_currentTrack != null && _npsmInfo != null && Equals(prevTrack, _currentTrack) && (_npsmInfo.TrackProgress == 0 || _npsmInfo.TrackProgress == _currentTrack.Duration))
                            {
                                if (TrackChanged?.HasListeners() == true)
                                {
                                    if (!UseMediaSession)
                                    {
                                        TrackChanged?.Invoke(this, GetAMPlayerInfo().ToPlayerStateModel(true));
                                    }
                                    else
                                    {
                                        TrackChanged?.Invoke(this, _npsmInfo.ToPlayerStateModel(true));
                                    }
                                }
                            }
                        });
                    }
                    break;
            }
        }
    }
}
