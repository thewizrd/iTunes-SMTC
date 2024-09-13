using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace iTunes.SMTC.AppleMusic.Model
{
    public record EventMessage(
        [property: JsonConverter(typeof(StringEnumConverter))]
        EventType EventType,
        object Payload
    );

    public enum EventType
    {
        Unknown,
        TrackChange,
        PlayerStateChanged,
        ArtworkChanged,
        VolumeChanged,
        PlaybackPositionChanged
    }
}
