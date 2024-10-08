﻿using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using iTunes.SMTC.AppleMusic.Model;
using iTunes.SMTC.Utils;
using System.Diagnostics;


#if DEBUG || UNPACKAGEDDEBUG
using System.Text;
#endif
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController
    {
        internal enum AppleMusicControlButtons
        {
            Shuffle,
            SkipBack,
            PlayPauseStop,
            SkipForward,
            Repeat
        }

        private Window? FindAppleMusicWindow()
        {
            try
            {
                var processes = Process.GetProcessesByName("AppleMusic");

                // Check if app window is available and responding
                var process = processes.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero && p.Responding);

                if (process != null)
                {
                    var app = FlaUI.Core.Application.Attach(process);

                    if (Wait.UntilResponsive(app.MainWindowHandle, TimeSpan.FromSeconds(5)))
                    {
                        using var automation = new UIA3Automation();
                        var window = app.GetMainWindow(automation, waitTimeout: TimeSpan.FromSeconds(5));

                        if ((window?.Name == "Apple Music" && window.ClassName == "WinUIDesktopWin32WindowClass") ||
                            window?.Name == "MiniPlayer" || window?.Name == "Mini Player")
                        {
                            return window;
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                return null;
            }
            return null;
        }

        private AMPlayerInfo GetAMPlayerInfo()
        {
            // Poll for Apple Music window
            var window = FindAppleMusicWindow();
            var info = new AMPlayerInfo();

            if (window != null)
            {
                try
                {
                    // Main Window Content
                    var content = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));

#if DEBUG || UNPACKAGEDDEBUG
                    //var volumeBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("VolumeButton"))?.AsButton();
                    //volumeBtn?.Click();
                    //Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(5));
                    //LookForChildrenAndDescendants(window);
#endif

                    var shuffleBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("ShuffleButton"))?.AsToggleButton();
                    var skipBackBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipBack"))?.AsButton();
                    var playPauseStopBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_PlayPauseStop"))?.AsButton();
                    var skipFwdBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipForward"))?.AsButton();
                    var repeatBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("RepeatButton"))?.AsToggleButton();

                    //var thumbnailHoverGrid = content.FindFirstDescendant(cf => cf.ByAutomationId("ThumbnailHoverGrid"));
                    var mediaTextDetails = content.FindAllDescendants(cf => cf.ByAutomationId("ScrollingText").And(cf.ByClassName("TextBlock")));
                    var mediaDetailCount = mediaTextDetails?.Length ?? 0;

                    if (mediaDetailCount == 2)
                    {
                        var artistAlbumTitles = mediaTextDetails[1].Name?.Split(" — ");

                        info.TrackData = new TrackMetadata()
                        {
                            Name = mediaTextDetails[0].Name,
                            Artist = artistAlbumTitles.FirstOrDefault(),
                            Album = artistAlbumTitles.ElementAtOrDefault(1)
                        };

                        /*
                        try
                        {
                            info.TrackData.Artwork = thumbnailHoverGrid?.Capture()?.ToBytes();
                        }
                        catch { }
                        */
                    }

                    info.ShuffleEnabled = shuffleBtn != null && shuffleBtn.IsAvailable && shuffleBtn.IsEnabled && shuffleBtn.ToggleState == FlaUI.Core.Definitions.ToggleState.On;

                    info.SkipBackEnabled = skipBackBtn != null && skipBackBtn.IsAvailable && skipBackBtn.IsEnabled;

                    if (playPauseStopBtn != null && playPauseStopBtn.IsAvailable && playPauseStopBtn.IsEnabled)
                    {
                        info.IsPlaying = (playPauseStopBtn.Name == "Pause" || playPauseStopBtn.Name == "Stop");
                        info.PlayPauseStopButtonState = playPauseStopBtn.Name switch
                        {
                            "Play" => PlayPauseStopButtonState.Play,
                            "Pause" => PlayPauseStopButtonState.Pause,
                            "Stop" => PlayPauseStopButtonState.Stop,
                            _ => PlayPauseStopButtonState.Unknown,
                        };
                    }
                    else
                    {
                        info.IsPlaying = false;
                        info.PlayPauseStopButtonState = PlayPauseStopButtonState.Unknown;
                    }

                    info.SkipForwardEnabled = skipFwdBtn != null && skipFwdBtn.IsAvailable && skipFwdBtn.IsEnabled;

                    if (repeatBtn != null && repeatBtn.IsAvailable && repeatBtn.IsEnabled)
                    {
                        info.RepeatMode = repeatBtn.ToggleState switch
                        {
                            FlaUI.Core.Definitions.ToggleState.Off => MediaPlaybackAutoRepeatMode.None,
                            FlaUI.Core.Definitions.ToggleState.Indeterminate => MediaPlaybackAutoRepeatMode.Track,
                            FlaUI.Core.Definitions.ToggleState.On => MediaPlaybackAutoRepeatMode.List,
                            _ => MediaPlaybackAutoRepeatMode.None,
                        };
                    }
                    else
                    {
                        info.RepeatMode = MediaPlaybackAutoRepeatMode.None;
                    }

                    // Track Duration info
                    var progressSlider = content.FindFirstDescendant(cf => cf.ByAutomationId("LCDScrubber").Or(new BoolCondition(IsMiniPlayer(window)).And(cf.ByAutomationId("Scrubber"))))?.AsSlider();
                    if (progressSlider != null)
                    {
                        // Focus on slider to get time and duration
                        //progressSlider.Focus();
                        info.SeekEnabled = progressSlider.IsAvailable && progressSlider.IsEnabled;

                        // Grab duration from slider (in seconds)
                        if (info.TrackData != null)
                            info.TrackData.Duration = (int)progressSlider.Maximum;
                        info.TrackProgress = (int)progressSlider.Value;

                        /*
                        var currentTime = content.FindFirstDescendant(cf => cf.ByAutomationId("CurrentTime"));
                        var duration = content.FindFirstDescendant(cf => cf.ByAutomationId("Duration"));

                        if (!string.IsNullOrWhiteSpace(currentTime?.Name) && !string.IsNullOrWhiteSpace(duration?.Name))
                        {
                            // Time format: x:xx; x:xx:xx
                            var currentTimeDur = ParseTime(currentTime.Name);
                            var remainingDuration = ParseTime(duration.Name);

                            var totalDuration = currentTimeDur + remainingDuration.Negate();

                            info.TrackData.Duration = (int)totalDuration.TotalSeconds;
                            info.TrackProgress = (int)currentTimeDur.TotalSeconds;
                        }
                        */
                    }
                    else
                    {
                        info.SeekEnabled = false;
                    }

                    if (content.FindFirstDescendant(cf => cf.ByName("LIVE")) is not null)
                    {
                        info.IsRadio = true;
                    }

                    // Volume slider
                    /*
                    var volumeBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("VolumeButton"))?.AsButton();
                    if (volumeBtn != null)
                    {
                        var popupHost = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.PopupWindowSiteBridge"));
                        var volumeFlyout = popupHost?.FindFirstDescendant(cf => cf.ByAutomationId("VolumeFlyout"));
                        var volumeSlider = volumeFlyout?.FindFirstDescendant(cf => cf.ByAutomationId("VolumeSlider"))?.AsSlider();

                        if (volumeSlider == null)
                        {
                            // Click button to bring up volume flyout
                            volumeBtn.Click();
                            Wait.UntilInputIsProcessed();
                            popupHost ??= window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.PopupWindowSiteBridge"));
                            volumeFlyout = popupHost?.FindFirstDescendant(cf => cf.ByAutomationId("VolumeFlyout"));
                            volumeSlider = volumeFlyout?.FindFirstDescendant(cf => cf.ByAutomationId("VolumeSlider"))?.AsSlider();
                        }

                        if (volumeSlider != null)
                        {
                            info.VolumeState ??= new VolumeState();
                            info.VolumeState.Volume = volumeSlider.Value;
                            info.VolumeState.IsMuted = volumeSlider.Value == 0;
                        }
                    }
                    */
                    info.VolumeState = _currentVolume;
                }
                catch (Exception)
                {
                    // may occur if window is no longer available or not responsive
                }
            }

            return info;
        }

        /*
        private static TimeSpan ParseTime(string duration)
        {
            // Time formats: x:xx; x:xx:xx
            var separatorCount = duration.Count(c => c == ':');
            var negate = duration.StartsWith("-");
            if (negate) duration = duration.Substring(1);

            TimeSpan ts;

            if (separatorCount == 1)
            {
                // m:ss
                ts = TimeSpan.ParseExact(duration, "m\\:ss", CultureInfo.InvariantCulture);
            }
            else if (separatorCount == 2)
            {
                // h:mm:ss
                if (duration.StartsWith("-"))
                {
                    ts = TimeSpan.ParseExact(duration, "h\\:mm\\:ss", CultureInfo.InvariantCulture);
                }
                else
                {
                    ts = TimeSpan.ParseExact(duration, "h\\:mm\\:ss", CultureInfo.InvariantCulture);
                }
            }
            else
            {
                ts = TimeSpan.Zero;
            }

            if (negate)
                return ts.Negate();

            return ts;
        }
        */

        private void UpdateSMTCDisplay(AMPlayerInfo info)
        {
            AMDispatcher.TryEnqueue(async () =>
            {
                if (info != null)
                {
                    var playerStateChanged = (_systemMediaTransportControls.ShuffleEnabled != info.ShuffleEnabled) ||
                        (_systemMediaTransportControls.AutoRepeatMode != info.RepeatMode) ||
                        (_systemMediaTransportControls.IsPreviousEnabled != info.SkipBackEnabled) ||
                        (_systemMediaTransportControls.IsNextEnabled != info.SkipForwardEnabled);

                    _systemMediaTransportControls.PlaybackStatus = info.IsPlaying ? MediaPlaybackStatus.Playing : (!string.IsNullOrEmpty(info.TrackData?.Name) ? MediaPlaybackStatus.Paused : MediaPlaybackStatus.Closed);
                    _systemMediaTransportControls.IsEnabled = !string.IsNullOrEmpty(info?.TrackData?.Name);

                    _systemMediaTransportControls.ShuffleEnabled = info.ShuffleEnabled;
                    _systemMediaTransportControls.AutoRepeatMode = info.RepeatMode;

                    _systemMediaTransportControls.IsPreviousEnabled = info.SkipBackEnabled;
                    _systemMediaTransportControls.IsNextEnabled = info.SkipForwardEnabled;

                    _systemMediaTransportControls.IsPauseEnabled = true;
                    _systemMediaTransportControls.IsPlayEnabled = true;
                    _systemMediaTransportControls.IsStopEnabled = true;

                    var prevTrack = _currentTrack;
                    var trackChanged = _currentTrack == null || !Equals(info.TrackData, _currentTrack);
                    var volumeChanged = !Equals(info.VolumeState, _currentVolume);

                    if (trackChanged)
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

                            // Update artwork
                            if (info.TrackData.Artwork is byte[] buf)
                            {
                                ResetArtworkToken();
                                var token = artworkCts.Token;

                                try
                                {
                                    ArtworkDispatcher.TryEnqueue(async () =>
                                    {
                                        await Task.Delay(500);

                                        if (token.IsCancellationRequested) return;

                                        using var memoryStream = new MemoryStream(buf, false);
                                        await SaveArtwork(memoryStream);
                                        updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));
                                        updater.Update();

                                        if (token.IsCancellationRequested) return;

                                        if (ArtworkChanged?.HasListeners() == true)
                                        {
                                            ArtworkChanged?.Invoke(this, new ArtworkModel { Artwork = await updater.Thumbnail.ToBytes() });
                                        }
                                    });
                                }
                                catch
                                {
                                    ArtworkDispatcher.TryEnqueue(async () =>
                                    {
                                        await Task.Delay(500);

                                        if (token.IsCancellationRequested) return;

                                        await SaveArtwork(null);
                                    });
                                }
                            }
                            else if (!UseMediaSession && MediaPlaybackSource != null)
                            {
                                ResetArtworkToken();
                                var token = artworkCts.Token;

                                try
                                {
                                    info.TrackData.Artwork = _npsmInfo?.TrackData?.Artwork;
                                    ArtworkDispatcher.TryEnqueue(async () =>
                                    {
                                        await Task.Delay(500);

                                        if (token.IsCancellationRequested) return;

                                        await SaveArtwork(MediaPlaybackSource.GetThumbnailStream());
                                        updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));
                                        updater.Update();

                                        if (token.IsCancellationRequested) return;

                                        if (ArtworkChanged?.HasListeners() == true)
                                        {
                                            ArtworkChanged?.Invoke(this, new ArtworkModel { Artwork = await updater.Thumbnail.ToBytes() });
                                        }
                                    });
                                }
                                catch
                                {
                                    ArtworkDispatcher.TryEnqueue(async () =>
                                    {
                                        await Task.Delay(500);

                                        if (token.IsCancellationRequested) return;

                                        await SaveArtwork(null);
                                    });
                                }
                            }
                            else
                            {
                                ResetArtworkToken();
                                var token = artworkCts.Token;

                                ArtworkDispatcher.TryEnqueue(async () =>
                                {
                                    await Task.Delay(500);

                                    if (token.IsCancellationRequested) return;

                                    await SaveArtwork(null);
                                });
                            }
                        }
                        else
                        {
                            if (!_metadataEmpty)
                            {
                                updater.Type = MediaPlaybackType.Music;
                                updater.MusicProperties.Artist = "Media Controller";

                                // Remove artwork
                                ResetArtworkToken();
                                var token = artworkCts.Token;

                                ArtworkDispatcher.TryEnqueue(async () =>
                                {
                                    await Task.Delay(500);

                                    if (token.IsCancellationRequested) return;

                                    await SaveArtwork(null);
                                });

                                try
                                {
                                    updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));
                                }
                                catch { }
                            }

                            _metadataEmpty = true;
                        }

                        updater.Update();
                    }

                    if (info.TrackData != null)
                    {
                        _systemMediaTransportControls.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties()
                        {
                            StartTime = TimeSpan.Zero,
                            EndTime = TimeSpan.FromSeconds(info.TrackData.Duration),
                            Position = TimeSpan.FromSeconds(info.TrackProgress)
                        });
                    }

                    if ((trackChanged || _isPlaying != info.IsPlaying))
                    {
                        if (Settings.ShowTrackToast)
                        {
                            ShowToastNotification(info.TrackData);
                        }

                        if (trackChanged && (prevTrack != null || _currentTrack != null))
                        {
                            if (TrackChanged?.HasListeners() == true)
                            {
                                TrackChanged?.Invoke(this, info.ToPlayerStateModel(true));
                            }
                        }
                        else if (_isPlaying != info.IsPlaying)
                        {
                            if (PlayerStateChanged?.HasListeners() == true)
                            {
                                PlayerStateChanged?.Invoke(this, info.ToPlayerStateModel(false));
                            }
                        }
                    }
                    else if (playerStateChanged)
                    {
                        if (PlayerStateChanged?.HasListeners() == true)
                        {
                            PlayerStateChanged?.Invoke(this, info.ToPlayerStateModel(false));
                        }
                    }
                    else if (volumeChanged)
                    {
                        if (VolumeStateChanged?.HasListeners() == true)
                        {
                            VolumeStateChanged?.Invoke(this, info.VolumeState);
                        }
                    }

                    _isPlaying = info.IsPlaying;
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

                    _systemMediaTransportControls.UpdateTimelineProperties(new SystemMediaTransportControlsTimelineProperties());

                    if (_isPlaying || !_metadataEmpty)
                    {
                        if (TrackChanged?.HasListeners() == true)
                        {
                            TrackChanged?.Invoke(this, new PlayerStateModel());
                        }
                    }

                    _isPlaying = false;
                    _metadataEmpty = true;
                }
            });
        }

        private void UpdateSMTCExtras(AMPlayerInfo info)
        {
            if (info != null)
            {
                var extrasChanged = (_systemMediaTransportControls.ShuffleEnabled != info.ShuffleEnabled) || (_systemMediaTransportControls.AutoRepeatMode != info.RepeatMode);
                var volumeChanged = !Equals(info.VolumeState, _currentVolume);

                _systemMediaTransportControls.ShuffleEnabled = info.ShuffleEnabled;
                _systemMediaTransportControls.AutoRepeatMode = info.RepeatMode;

                if (_npsmInfo != null)
                {
                    extrasChanged = extrasChanged || (_npsmInfo.IsRadio != info.IsRadio);

                    _npsmInfo.ShuffleEnabled = info.ShuffleEnabled;
                    _npsmInfo.RepeatMode = info.RepeatMode;
                    _npsmInfo.IsRadio = info.IsRadio;
                    _npsmInfo.SeekEnabled = info.SeekEnabled;
                }

                if (extrasChanged)
                {
                    if (PlayerStateChanged?.HasListeners() == true)
                    {
                        PlayerStateChanged?.Invoke(this, info.ToPlayerStateModel(false));
                    }
                }

                if (volumeChanged)
                {
                    if (VolumeStateChanged?.HasListeners() == true)
                    {
                        VolumeStateChanged?.Invoke(this, info.VolumeState);
                    }
                }
            }
            else
            {
                _systemMediaTransportControls.ShuffleEnabled = false;
                _systemMediaTransportControls.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;

                if (_npsmInfo != null)
                {
                    _npsmInfo.ShuffleEnabled = false;
                    _npsmInfo.RepeatMode = MediaPlaybackAutoRepeatMode.None;
                    _npsmInfo.IsRadio = false;
                    _npsmInfo.SeekEnabled = false;
                }
            }
        }

        private void SendAMPlayerCommand(AppleMusicControlButtons button)
        {
            // Poll for Apple Music window
            var window = FindAppleMusicWindow();

            if (window != null)
            {
                // Main Window Content
                var content = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));

                switch (button)
                {
                    case AppleMusicControlButtons.Shuffle:
                        var shuffleBtn = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"))
                            ?.FindFirstDescendant(cf => cf.ByAutomationId("ShuffleButton"))
                            ?.AsToggleButton();
                        shuffleBtn?.Toggle();

                        // Update button state
                        AMDispatcher.TryEnqueue(() =>
                        {
                            _systemMediaTransportControls.ShuffleEnabled = shuffleBtn != null && shuffleBtn.IsAvailable && shuffleBtn.IsEnabled && shuffleBtn.ToggleState == FlaUI.Core.Definitions.ToggleState.On;
                        });
                        break;
                    case AppleMusicControlButtons.SkipBack:
                        window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"))
                            ?.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipBack"))
                            ?.AsButton()
                            ?.Click();
                        break;
                    case AppleMusicControlButtons.PlayPauseStop:
                        window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"))
                            ?.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_PlayPauseStop"))
                            ?.AsButton()
                            ?.Click();
                        break;
                    case AppleMusicControlButtons.SkipForward:
                        window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"))
                            ?.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipForward"))
                            ?.AsButton()
                            ?.Click();
                        break;
                    case AppleMusicControlButtons.Repeat:
                        var repeatBtn = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"))
                            ?.FindFirstDescendant(cf => cf.ByAutomationId("RepeatButton"))
                            ?.AsToggleButton();
                        repeatBtn?.Toggle();

                        // Update button state
                        AMDispatcher.TryEnqueue(() =>
                        {
                            _systemMediaTransportControls.AutoRepeatMode = repeatBtn.ToggleState switch
                            {
                                FlaUI.Core.Definitions.ToggleState.Off => MediaPlaybackAutoRepeatMode.None,
                                FlaUI.Core.Definitions.ToggleState.Indeterminate => MediaPlaybackAutoRepeatMode.Track,
                                FlaUI.Core.Definitions.ToggleState.On => MediaPlaybackAutoRepeatMode.List,
                                _ => MediaPlaybackAutoRepeatMode.None,
                            };
                        });
                        break;
                }
            }
        }

        private void UpdateAMPlayerPlaybackPosition(TimeSpan requestedPlaybackPosition)
        {
            UpdateAMPlayerPlaybackPosition(requestedPlaybackPosition.TotalSeconds);
        }

        internal void UpdateAMPlayerPlaybackPosition(double timeInSeconds)
        {
            // Poll for Apple Music window
            var window = FindAppleMusicWindow();

            if (window != null)
            {
                // Main Window Content
                var content = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));
                // Progress slider
                var progressSlider = content?.FindFirstDescendant(cf => cf.ByAutomationId("LCDScrubber").Or(new BoolCondition(IsMiniPlayer(window)).And(cf.ByAutomationId("Scrubber"))))?.AsSlider();

                if (progressSlider != null)
                {
                    // Set slider value (time in seconds)
                    progressSlider.Value = timeInSeconds;

                    if (PlaybackPositionChanged?.HasListeners() == true)
                    {
                        PlaybackPositionChanged?.Invoke(this, new TrackModel() { Progress = (int)timeInSeconds });
                    }
                }
            }
        }

        private static bool IsMiniPlayer(Window window)
        {
            return window?.Name == "MiniPlayer" || window?.Name == "Mini Player";
        }

#if DEBUG || UNPACKAGEDDEBUG
        private void LookForChildrenAndDescendants(AutomationElement content)
        {
            if (content != null)
            {
                var descs = content.FindAllDescendants();
                var childes = content.FindAllChildren();

                void PrintItemInfo(AutomationElement item)
                {
                    var sb = new StringBuilder();

                    try
                    {
                        sb.Append($"| Name: {item.Name} | ");
                    }
                    catch { }
                    try
                    {
                        sb.Append($"| ID: {item.AutomationId} | ");
                    }
                    catch { }
                    try
                    {
                        sb.Append($"Class: {item.ClassName} | ");
                    }
                    catch { }
                    try
                    {
                        sb.Append($"Parent: {item.Parent?.Name} | ");
                    }
                    catch { }
                    try
                    {
                        sb.Append($"ParentClass: {item.Parent?.ClassName} | ");
                    }
                    catch { }

                    Trace.WriteLine(sb.ToString());
                }

                Trace.WriteLine("===================Descendants===================");
                foreach (var item in descs)
                {
                    PrintItemInfo(item);
                }

                Trace.WriteLine("===================Children===================");
                foreach (var item in childes)
                {
                    PrintItemInfo(item);
                }
            }
        }
#endif
    }
}
