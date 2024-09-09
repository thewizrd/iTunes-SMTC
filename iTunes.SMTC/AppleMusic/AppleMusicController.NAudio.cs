using iTunes.SMTC.Utils;
using Microsoft.AppCenter.Crashes;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Diagnostics;

namespace iTunes.SMTC.AppleMusic
{
    public partial class AppleMusicController : IMMNotificationClient, IAudioSessionEventsHandler
    {
        private MMDeviceEnumerator MMDeviceEnumerator;
        private MMDevice MMDevice;
        private AudioSessionManager MMAudioSessionManager;
        private AudioSessionControl MMAudioSession;

        private void StartNAudioService()
        {
            if (MMDeviceEnumerator != null)
            {
                StopNAudioService();
            }

            if (Environment.OSVersion.Version.Major >= 6)
            {
                try
                {
                    // This functionality is only supported on Windows Vista or newer.
                    MMDeviceEnumerator ??= new MMDeviceEnumerator();
                    MMDeviceEnumerator.RegisterEndpointNotificationCallback(this);
                    ReloadSession(MMDeviceEnumerator);
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                }
            }
        }

        private void ReloadSession(MMDeviceEnumerator deviceEnumerator)
        {
            UnloadAudioSession();
            if (deviceEnumerator != null)
            {
                if (deviceEnumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                {
                    MMDevice = deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                }
                else
                {
                    MMDevice = null;
                }

                if (MMDevice != null)
                {
                    MMAudioSessionManager = MMDevice.AudioSessionManager;
                    MMAudioSessionManager.OnSessionCreated += MMAudioSessionManager_OnSessionCreated;

                    var amplProcess = Process.GetProcessesByName("AMPLibraryAgent")?.FirstOrDefault();

                    if (amplProcess != null)
                    {
                        var sessions = MMAudioSessionManager.Sessions;

                        for (int i = 0; i < sessions.Count; i++)
                        {
                            var session = sessions[i];

                            if (session.GetProcessID == amplProcess.Id)
                            {
                                MMAudioSession = session;
                                break;
                            }
                        }

                        LoadAudioSession();
                    }
                }
            }
        }

        private void MMAudioSessionManager_OnSessionCreated(object sender, IAudioSessionControl newSession)
        {
            var amplProcess = Process.GetProcessesByName("AMPLibraryAgent")?.FirstOrDefault();

            if (amplProcess != null && newSession is AudioSessionControl ctrl && ctrl.GetProcessID == amplProcess.Id)
            {
                UnloadAudioSession();

                MMAudioSession = ctrl;

                LoadAudioSession();
            }
        }

        private void LoadAudioSession()
        {
            if (MMAudioSession != null)
            {
                MMAudioSession.RegisterEventClient(this);

                UpdateVolumeState(MMAudioSession.SimpleAudioVolume);

                if (VolumeStateChanged?.HasListeners() == true)
                {
                    VolumeStateChanged?.Invoke(this, _currentVolume);
                }
            }
        }

        private void StopNAudioService()
        {
            if (MMDeviceEnumerator != null)
            {
                try
                {
                    MMDeviceEnumerator.UnregisterEndpointNotificationCallback(this);
                    MMDeviceEnumerator?.Dispose();
                }
                catch (Exception ex)
                {
                    Crashes.TrackError(ex);
                }
            }

            MMDeviceEnumerator = null;
            MMDevice = null;
            MMAudioSessionManager = null;
            MMAudioSession = null;
        }

        private void UnloadAudioSession()
        {
            try
            {
                MMAudioSession?.UnRegisterEventClient(this);
                MMAudioSession?.Dispose();

                if (MMAudioSessionManager != null)
                {
                    MMAudioSessionManager.OnSessionCreated -= MMAudioSessionManager_OnSessionCreated;
                    MMAudioSessionManager?.Dispose();
                }

                MMDevice?.Dispose();
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
            }

            MMDevice = null;
            MMAudioSessionManager = null;
            MMAudioSession = null;
        }

        private void UpdateVolumeState(SimpleAudioVolume audioVolume)
        {
            _currentVolume ??= new Model.VolumeState();
            _currentVolume.Volume = audioVolume.Volume;
            _currentVolume.IsMuted = audioVolume.Mute;
        }

        public void UpdateVolume(float volume)
        {
            if (MMAudioSession != null)
            {
                MMAudioSession.SimpleAudioVolume.Volume = volume;
            }
        }

        public void Mute(bool isMuted = true)
        {
            if (MMAudioSession != null)
            {
                MMAudioSession.SimpleAudioVolume.Mute = isMuted;
            }
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

        public void OnDeviceAdded(string pwstrDeviceId) { }

        public void OnDeviceRemoved(string deviceId) { }

        public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
        {
            if (flow == DataFlow.Render && role == Role.Multimedia)
            {
                ReloadSession(MMDeviceEnumerator);
            }
        }

        public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

        public void OnVolumeChanged(float volume, bool isMuted)
        {
            _currentVolume ??= new Model.VolumeState();
            _currentVolume.Volume = volume;
            _currentVolume.IsMuted = isMuted;

            if (VolumeStateChanged?.HasListeners() == true)
            {
                VolumeStateChanged?.Invoke(this, _currentVolume);
            }
        }

        public void OnDisplayNameChanged(string displayName) { }

        public void OnIconPathChanged(string iconPath) { }

        public void OnChannelVolumeChanged(uint channelCount, nint newVolumes, uint channelIndex) { }

        public void OnGroupingParamChanged(ref Guid groupingId) { }

        public void OnStateChanged(AudioSessionState state) { }

        public void OnSessionDisconnected(AudioSessionDisconnectReason disconnectReason)
        {
            UnloadAudioSession();
        }
    }
}
