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
    }

    public enum PlayPauseStopButtonState
    {
        Unknown,
        Play,
        Pause,
        Stop
    }

    public class TrackMetadata
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TrackMetadata metadata &&
                   Name == metadata.Name &&
                   Artist == metadata.Artist &&
                   Album == metadata.Album;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Artist, Album);
        }
    }
}
