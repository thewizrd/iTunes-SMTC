using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using iTunes.SMTC.AppleMusic.Model;
using System.Diagnostics;
using System.Text;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController
    {
        private Window? FindAppleMusicWindow()
        {
            try
            {
                var process = Process.GetProcessesByName("AppleMusic").FirstOrDefault();

                if (process != null)
                {
                    var app = FlaUI.Core.Application.Attach(process);

                    var window = app.GetMainWindow(new UIA3Automation(), waitTimeout: TimeSpan.FromSeconds(5));

                    if (window?.Name == "Apple Music" && window.ClassName == "WinUIDesktopWin32WindowClass")
                    {
                        return window;
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

                //LookForChildrenAndDescendants(content);

                var shuffleBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("ShuffleButton"))?.AsToggleButton();
                var skipBackBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipBack"))?.AsButton();
                var playPauseStopBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_PlayPauseStop"))?.AsButton();
                var skipFwdBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("TransportControl_SkipForward"))?.AsButton();
                var repeatBtn = content.FindFirstDescendant(cf => cf.ByAutomationId("RepeatButton"))?.AsToggleButton();

                var mediaTextDetails = content.FindAllDescendants(cf => cf.ByAutomationId("ScrollingText"));
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
                        FlaUI.Core.Definitions.ToggleState.Off => Windows.Media.MediaPlaybackAutoRepeatMode.None,
                        FlaUI.Core.Definitions.ToggleState.On => Windows.Media.MediaPlaybackAutoRepeatMode.Track,
                        FlaUI.Core.Definitions.ToggleState.Indeterminate => Windows.Media.MediaPlaybackAutoRepeatMode.List,
                        _ => Windows.Media.MediaPlaybackAutoRepeatMode.None,
                    };
                }
                else
                {
                    info.RepeatMode = Windows.Media.MediaPlaybackAutoRepeatMode.None;
                }
            }

            return info;
        }

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
    }
}
