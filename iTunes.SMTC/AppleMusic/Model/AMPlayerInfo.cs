using Windows.Media;

namespace iTunes.SMTC.AppleMusic.Model
{
    public class AMPlayerInfo
    {
        public TrackMetadata TrackData { get; set; }

        public bool IsPlaying { get; set; }
        public PlayPauseStopButtonState PlayPauseStopButtonState { get; set; } = PlayPauseStopButtonState.Unknown;

        public bool ShuffleEnabled { get; set; }
        public MediaPlaybackAutoRepeatMode RepeatMode { get; set; } = MediaPlaybackAutoRepeatMode.None;

        public bool SkipBackEnabled { get; set; }
        public bool SkipForwardEnabled { get; set; }

        /// <summary>
        /// Current progress on track in seconds
        /// </summary>
        public int TrackProgress { get; set; }
    }
}
