namespace iTunes.SMTC.AppleMusic.Model
{
    public class TrackMetadata
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }

        /// <summary>
        /// Duration of track in seconds
        /// </summary>
        public int Duration { get; set; } = -1;

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

        public TrackMetadata Copy()
        {
            return this.MemberwiseClone() as TrackMetadata;
        }
    }
}
