namespace iTunes.SMTC.AppleMusic.Model
{
    public sealed class TrackMetadata : IDisposable
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public Bitmap Artwork { get; set; }

        /// <summary>
        /// Duration of track in seconds
        /// </summary>
        public int Duration { get; set; } = -1;

        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Artist) && string.IsNullOrEmpty(Album);

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

        public void Dispose()
        {
            Artwork?.Dispose();
        }
    }
}
