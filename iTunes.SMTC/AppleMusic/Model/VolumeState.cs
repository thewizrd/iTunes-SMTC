namespace iTunes.SMTC.AppleMusic.Model
{
    public sealed class VolumeState
    {
        public double Volume { get; set; }
        public bool IsMuted { get; set; }

        public override bool Equals(object obj)
        {
            return obj is VolumeState state &&
                   Volume == state.Volume &&
                   IsMuted == state.IsMuted;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Volume, IsMuted);
        }
    }
}
