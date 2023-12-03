using iTunesLib;
using System.Runtime.InteropServices;

namespace iTunes.SMTC.iTunes.Model
{
    public class TrackMetadata : IDisposable
    {
        public int DatabaseID { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public uint TrackNumber { get; set; }

        /// <summary>
        /// Duration of track in seconds
        /// </summary>
        public int Duration { get; set; }
        /// <summary>
        /// Start time of track in seconds
        /// </summary>
        public int StartTime { get; set; }
        /// <summary>
        /// End time of track in seconds
        /// </summary>
        public int EndTime { get; set; }

        public IITArtwork Artwork { get; set; }

        #region Disposable
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                if (Artwork != null)
                {
                    Marshal.FinalReleaseComObject(Artwork);
                }
                // TODO: set large fields to null
                Artwork = null;

                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~TrackMetadata()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
