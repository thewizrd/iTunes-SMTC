﻿using iTunes.SMTC.AppleMusic.Model;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController
    {
        public event EventHandler<PlayerStateModel> TrackChanged;
        public event EventHandler<PlayerStateModel> PlayerStateChanged;
        public event EventHandler<VolumeState> VolumeStateChanged;
        public event EventHandler<ArtworkModel> ArtworkChanged;
        public event EventHandler<TrackModel> PlaybackPositionChanged;
    }
}
