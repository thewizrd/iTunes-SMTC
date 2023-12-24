using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using iTunes.SMTC.AppleMusic.Model;
using System.Diagnostics;
using System.Drawing.Imaging;

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

                        if (window?.Name == "Apple Music" && window.ClassName == "WinUIDesktopWin32WindowClass")
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
                // Main Window Content
                var content = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));

#if DEBUG || UNPACKAGEDDEBUG
                //LookForChildrenAndDescendants(content.FindFirstDescendant(cf => cf.ByAutomationId("LCD")));
#endif

                var shuffleBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("ShuffleButton"))?.AsToggleButton();
                var skipBackBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipBack"))?.AsButton();
                var playPauseStopBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_PlayPauseStop"))?.AsButton();
                var skipFwdBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipForward"))?.AsButton();
                var repeatBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("RepeatButton"))?.AsToggleButton();

                var thumbnailHoverGrid = content.FindFirstDescendant(cf => cf.ByAutomationId("ThumbnailHoverGrid"));
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

                    try
                    {
                        info.TrackData.Artwork = thumbnailHoverGrid?.Capture();
                    }
                    catch { }
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
                var progressSlider = content.FindFirstDescendant(cf => cf.ByAutomationId("LCDScrubber"))?.AsSlider();
                if (progressSlider != null)
                {
                    // Focus on slider to get time and duration
                    //progressSlider.Focus();

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

                    var trackChanged = _currentTrack == null || !Equals(info.TrackData, _currentTrack);

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
                            if (info.TrackData.Artwork != null && !info.TrackData.Artwork.Size.IsEmpty)
                            {
                                try
                                {
                                    var memoryStream = new MemoryStream();
                                    info.TrackData.Artwork.Save(memoryStream, ImageFormat.Jpeg);
                                    SaveArtwork(memoryStream);
                                    updater.Thumbnail = RandomAccessStreamReference.CreateFromFile(await StorageFile.GetFileFromPathAsync(_artworkUri.LocalPath));
                                }
                                catch
                                {
                                    SaveArtwork(null);
                                }
                            }
                            else
                            {
                                SaveArtwork(null);
                            }
                        }
                        else
                        {
                            if (!_metadataEmpty)
                            {
                                updater.Type = MediaPlaybackType.Music;
                                updater.MusicProperties.Artist = "Media Controller";

                                // Remove artwork
                                SaveArtwork(null);

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

                    if ((trackChanged || _isPlaying != info.IsPlaying) && Settings.ShowTrackToast)
                    {
                        ShowToastNotification(info.TrackData);
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

                    _isPlaying = false;
                    _metadataEmpty = true;
                }
            });
        }

        private void UpdateSMTCExtras(AMPlayerInfo info)
        {
            if (info != null)
            {
                _systemMediaTransportControls.ShuffleEnabled = info.ShuffleEnabled;
                _systemMediaTransportControls.AutoRepeatMode = info.RepeatMode;
            }
            else
            {
                _systemMediaTransportControls.ShuffleEnabled = false;
                _systemMediaTransportControls.AutoRepeatMode = MediaPlaybackAutoRepeatMode.None;
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
            // Poll for Apple Music window
            var window = FindAppleMusicWindow();

            if (window != null)
            {
                // Main Window Content
                var content = window.FindFirstChild(cf => cf.ByClassName("Microsoft.UI.Content.DesktopChildSiteBridge"));
                // Progress slider
                var progressSlider = content?.FindFirstDescendant(cf => cf.ByAutomationId("LCDScrubber"))?.AsSlider();

                if (progressSlider != null)
                {
                    // Set slider value (time in seconds)
                    progressSlider.Value = requestedPlaybackPosition.TotalSeconds;
                }
            }
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
