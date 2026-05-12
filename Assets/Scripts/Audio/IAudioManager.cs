using HorrorGame.Core;
using UnityEngine;

namespace HorrorGame.Audio
{
    /// <summary>
    /// Public audio service contract exposed through the ServiceLocator.
    /// </summary>
    public interface IAudioManager : IService
    {
        bool IsMuted { get; }

        float MasterVolume { get; }

        void SetMuted(bool isMuted);

        void SetMasterVolume(float volume);

        void PlaySFX(string soundId, Vector3 position, float volume = 1f);

        void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f);

        void PlayOneShot(string soundId, Vector3 position, float volume = 1f);

        void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f);

        void PlayLoop(string loopId, string soundId, Vector3 position, float volume = 1f);

        void PlayLoop(string loopId, AudioClip clip, Vector3 position, float volume = 1f);

        bool IsLoopPlaying(string loopId);

        void StopLoop(string loopId);

        void StopAllLoops();
    }
}
