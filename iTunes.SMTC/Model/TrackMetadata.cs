using iTunesLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTunes.SMTC.Model
{
    public class TrackMetadata
    {
        public int DatabaseID { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public uint TrackNumber { get; set; }

        /// <summary>
        /// Duration of track in milliseconds
        /// </summary>
        public int Duration { get; set; }
        /// <summary>
        /// Start time of track in milliseconds
        /// </summary>
        public int StartTime { get; set; }
        /// <summary>
        /// End time of track in milliseconds
        /// </summary>
        public int EndTime { get; set; }

        public IITArtwork Artwork { get; set; }
    }
}
