using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Audio
{
    /// <summary>
    /// Test-friendly audio service that records requests without playing sound.
    /// </summary>
    public sealed class MockAudioManager : IAudioManager
    {
        private readonly List<AudioOneShotRequest> oneShotRequests = new();
        private readonly Dictionary<string, AudioLoopRequest> activeLoops = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<AudioOneShotRequest> OneShotRequests => oneShotRequests;

        public IReadOnlyDictionary<string, AudioLoopRequest> ActiveLoops => activeLoops;

        public int StopAllLoopsCallCount { get; private set; }

        public bool IsMuted { get; private set; }

        public float MasterVolume { get; private set; } = 1f;

        public void SetMuted(bool isMuted)
        {
            IsMuted = isMuted;
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
        }

        public void PlaySFX(string soundId, Vector3 position, float volume = 1f)
        {
            PlayOneShot(soundId, position, volume);
        }

        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            PlayOneShot(clip, position, volume);
        }

        public void PlayOneShot(string soundId, Vector3 position, float volume = 1f)
        {
            ValidateSoundId(soundId);
            oneShotRequests.Add(AudioOneShotRequest.FromId(soundId.Trim(), position, volume));
        }

        public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip), "Audio clip cannot be null.");
            }

            oneShotRequests.Add(AudioOneShotRequest.FromClip(clip, position, volume));
        }

        public void PlayLoop(string loopId, string soundId, Vector3 position, float volume = 1f)
        {
            ValidateLoopId(loopId);
            ValidateSoundId(soundId);
            AddLoop(AudioLoopRequest.FromId(loopId.Trim(), soundId.Trim(), position, volume));
        }

        public void PlayLoop(string loopId, AudioClip clip, Vector3 position, float volume = 1f)
        {
            ValidateLoopId(loopId);

            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip), "Audio clip cannot be null.");
            }

            AddLoop(AudioLoopRequest.FromClip(loopId.Trim(), clip, position, volume));
        }

        public bool IsLoopPlaying(string loopId)
        {
            ValidateLoopId(loopId);
            return activeLoops.ContainsKey(loopId.Trim());
        }

        public void StopLoop(string loopId)
        {
            ValidateLoopId(loopId);
            activeLoops.Remove(loopId.Trim());
        }

        public void StopAllLoops()
        {
            StopAllLoopsCallCount++;
            activeLoops.Clear();
        }

        public void ClearRecordedCalls()
        {
            oneShotRequests.Clear();
            activeLoops.Clear();
            StopAllLoopsCallCount = 0;
        }

        private void AddLoop(AudioLoopRequest request)
        {
            ValidateLoopId(request.LoopId);

            if (activeLoops.ContainsKey(request.LoopId))
            {
                throw new InvalidOperationException(
                    $"Audio loop '{request.LoopId}' is already playing. Stop it before starting another loop with the same id.");
            }

            activeLoops.Add(request.LoopId, request);
        }

        private static void ValidateSoundId(string soundId)
        {
            if (string.IsNullOrWhiteSpace(soundId))
            {
                throw new ArgumentException("Sound id cannot be empty.", nameof(soundId));
            }
        }

        private static void ValidateLoopId(string loopId)
        {
            if (string.IsNullOrWhiteSpace(loopId))
            {
                throw new ArgumentException("Loop id cannot be empty.", nameof(loopId));
            }
        }

        public readonly struct AudioOneShotRequest
        {
            private AudioOneShotRequest(string soundId, AudioClip clip, Vector3 position, float volume)
            {
                SoundId = soundId;
                Clip = clip;
                Position = position;
                Volume = Mathf.Clamp01(volume);
            }

            public string SoundId { get; }

            public AudioClip Clip { get; }

            public Vector3 Position { get; }

            public float Volume { get; }

            public bool UsesClip => Clip != null;

            public static AudioOneShotRequest FromId(string soundId, Vector3 position, float volume)
            {
                return new AudioOneShotRequest(soundId, null, position, volume);
            }

            public static AudioOneShotRequest FromClip(AudioClip clip, Vector3 position, float volume)
            {
                return new AudioOneShotRequest(null, clip, position, volume);
            }
        }

        public readonly struct AudioLoopRequest
        {
            private AudioLoopRequest(string loopId, string soundId, AudioClip clip, Vector3 position, float volume)
            {
                LoopId = loopId;
                SoundId = soundId;
                Clip = clip;
                Position = position;
                Volume = Mathf.Clamp01(volume);
            }

            public string LoopId { get; }

            public string SoundId { get; }

            public AudioClip Clip { get; }

            public Vector3 Position { get; }

            public float Volume { get; }

            public bool UsesClip => Clip != null;

            public static AudioLoopRequest FromId(string loopId, string soundId, Vector3 position, float volume)
            {
                return new AudioLoopRequest(loopId, soundId, null, position, volume);
            }

            public static AudioLoopRequest FromClip(string loopId, AudioClip clip, Vector3 position, float volume)
            {
                return new AudioLoopRequest(loopId, null, clip, position, volume);
            }
        }
    }
}
