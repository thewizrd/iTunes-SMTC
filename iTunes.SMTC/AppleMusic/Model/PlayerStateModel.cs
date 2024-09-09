using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Windows.Media;

namespace iTunes.SMTC.AppleMusic.Model
{
    public class PlayerStateModel
    {
        public bool IsPlaying { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PlayPauseStopButtonState PlayPauseStopButtonState { get; set; } = PlayPauseStopButtonState.Unknown;
        [JsonConverter(typeof(StringEnumConverter))]
        public MediaPlaybackAutoRepeatMode RepeatMode { get; set; } = MediaPlaybackAutoRepeatMode.None;
        public bool ShuffleEnabled { get; set; }
        public bool SkipBackEnabled { get; set; }
        public bool SkipForwardEnabled { get; set; }

        public byte[] Artwork { get; set; }
        public TrackModel TrackData { get; set; }

        public bool IsRadio { get; set; }

        public VolumeState VolumeState { get; set; }
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

    public static class PlayerStateModelExtensions
    {
        public static PlayerStateModel ToPlayerStateModel(this AMPlayerInfo playerInfo, bool includeArtwork = false)
        {
            if (playerInfo.TrackData != null)
            {
                var artwork = includeArtwork ? playerInfo.TrackData.Artwork : null;

                return new PlayerStateModel()
                {
                    IsPlaying = playerInfo.IsPlaying,
                    PlayPauseStopButtonState = playerInfo.PlayPauseStopButtonState,
                    ShuffleEnabled = playerInfo.ShuffleEnabled,
                    RepeatMode = playerInfo.RepeatMode,
                    SkipBackEnabled = playerInfo.SkipBackEnabled,
                    SkipForwardEnabled = playerInfo.SkipForwardEnabled,
                    TrackData = new TrackModel()
                    {
                        Name = playerInfo.TrackData?.Name,
                        Artist = playerInfo.TrackData?.Artist,
                        Album = playerInfo.TrackData?.Album,
                        Progress = playerInfo.TrackProgress,
                        Duration = playerInfo.TrackData?.Duration ?? 0,
                    },
                    Artwork = artwork,
                    IsRadio = playerInfo.IsRadio,
                    VolumeState = playerInfo.VolumeState,
                };
            }
            else
            {
                return new PlayerStateModel()
                {
                    IsPlaying = false
                };
            }
        }
        public static PlayerStateModel ToPlayerStateModel(this NPSMInfo playerInfo, bool includeArtwork = false)
        {
            if (playerInfo.TrackData != null)
            {
                var artwork = includeArtwork ? playerInfo.TrackData.Artwork : null;

                return new PlayerStateModel()
                {
                    IsPlaying = playerInfo.IsPlaying,
                    PlayPauseStopButtonState = playerInfo.IsPlaying ? PlayPauseStopButtonState.Pause : PlayPauseStopButtonState.Play,
                    ShuffleEnabled = playerInfo.ShuffleEnabled,
                    RepeatMode = playerInfo.RepeatMode,
                    SkipBackEnabled = playerInfo.IsPreviousEnabled,
                    SkipForwardEnabled = playerInfo.IsNextEnabled,
                    TrackData = new TrackModel()
                    {
                        Name = playerInfo.TrackData?.Name,
                        Artist = playerInfo.TrackData?.Artist,
                        Album = playerInfo.TrackData?.Album,
                        Progress = playerInfo.TrackProgress,
                        Duration = playerInfo.TrackData?.Duration ?? 0,
                    },
                    Artwork = artwork
                };
            }
            else
            {
                return new PlayerStateModel()
                {
                    IsPlaying = false
                };
            }
        }
    }
}
