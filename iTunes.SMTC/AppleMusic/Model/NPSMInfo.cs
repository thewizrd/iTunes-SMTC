using Windows.Media;

namespace iTunes.SMTC.AppleMusic.Model
{
    public class NPSMInfo
    {
        public TrackMetadata TrackData { get; set; } = new TrackMetadata();

        public bool IsPlaying { get; set; }

        public bool IsPlaybackPositionEnabled { get; set; }
        public bool IsPreviousEnabled { get; set; }
        public bool IsNextEnabled { get; set; }
        public bool IsPlayEnabled { get; set; }
        public bool IsPauseEnabled { get; set; }
        public bool IsStopEnabled { get; set; }

        /// <summary>
        /// Current progress on track in seconds
        /// </summary>
        public int TrackProgress { get; set; }
        public bool ShuffleEnabled { get; set; }
        public MediaPlaybackAutoRepeatMode RepeatMode { get; set; } = MediaPlaybackAutoRepeatMode.None;
    }
}
