using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Windows.Media;

namespace iTunes.SMTC.AppleMusic.Model
{
    public class PlayerStateModel
    {
        public bool IsPlaying { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayPauseStopButtonState PlayPauseStopButtonState { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public MediaPlaybackAutoRepeatMode RepeatMode { get; set; }
        public bool ShuffleEnabled { get; set; }
        public bool SkipBackEnabled { get; set; }
        public bool SkipForwardEnabled { get; set; }

        public byte[] Artwork { get; set; }
        public TrackModel TrackData { get; set; }
    }

    public class TrackModel
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        /// <summary>
        /// Duration of track in seconds
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// Current progress on track in seconds
        /// </summary>
        public int Progress { get; set; }
    }
}
