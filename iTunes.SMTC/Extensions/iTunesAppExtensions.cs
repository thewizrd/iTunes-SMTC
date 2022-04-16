using iTunesLib;
using iTunes.SMTC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AppCenter.Crashes;

namespace iTunes.SMTC.Extensions
{
    public static class iTunesAppExtensions
    {
        public static TrackMetadata GetMetadata(this IITTrack track)
        {
            var metadata = new TrackMetadata();

            try
            {
                metadata.DatabaseID = track?.TrackDatabaseID ?? 0;
                metadata.Artist = track?.Artist;
                metadata.Album = track?.Album;
                metadata.Name = track?.Name;
                metadata.TrackNumber = (uint) track?.TrackNumber;

                metadata.StartTime = track?.Start ?? 0;
                metadata.EndTime = track?.Finish ?? 0;
                metadata.Duration = track?.Duration ?? 0;

                metadata.Artwork = track?.Artwork?.Cast<IITArtwork>()?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

            return metadata;
        }

        public static void PlayOrResume(this iTunesApp _iTunesApp)
        {
            var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

            if (playerState == ITPlayerState.ITPlayerStateRewind || playerState == ITPlayerState.ITPlayerStateFastForward)
            {
                _iTunesApp?.Resume();
            }
            else
            {
                _iTunesApp?.Play();
            }
        }

        public static void PauseOrResume(this iTunesApp _iTunesApp)
        {
            var playerState = _iTunesApp?.PlayerState ?? ITPlayerState.ITPlayerStateStopped;

            if (playerState == ITPlayerState.ITPlayerStateRewind || playerState == ITPlayerState.ITPlayerStateFastForward)
            {
                _iTunesApp?.Resume();
            }
            else
            {
                _iTunesApp?.Pause();
            }
        }
    }
}
