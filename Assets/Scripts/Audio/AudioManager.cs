using System;
using System.Collections.Generic;
using HorrorGame.Core;
using UnityEngine;

namespace HorrorGame.Audio
{
    /// <summary>
    /// Unity AudioSource-backed implementation of the audio service.
    /// </summary>
    [DefaultExecutionOrder(ServiceExecutionOrder.FeatureServices)]
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour, IAudioManager
    {
        [SerializeField] private bool registerOnAwake = true;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] private bool isMuted;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(0f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 20f;
        [SerializeField] private ClipBinding[] clips = Array.Empty<ClipBinding>();

        private readonly Dictionary<string, AudioClip> clipById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LoopInstance> loops = new(StringComparer.OrdinalIgnoreCase);

        public bool IsMuted => isMuted;

        public float MasterVolume => masterVolume;

        private void Awake()
        {
            RebuildClipLibrary();

            if (dontDestroyOnLoad && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (registerOnAwake)
            {
                ServiceLocator.Register<IAudioManager>(this);
            }
        }

        public void SetMuted(bool isMuted)
        {
            this.isMuted = isMuted;
            RefreshLoopVolumes();
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            RefreshLoopVolumes();
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
            PlayOneShot(ResolveClip(soundId), position, volume);
        }

        public void PlayOneShot(AudioClip clip, Vector3 position, float volume = 1f)
        {
            ValidateClip(clip);

            if (isMuted || Mathf.Approximately(masterVolume, 0f))
            {
                return;
            }

            AudioSource source = CreateSource($"OneShot_{clip.name}", position);
            ConfigureSource(source, clip, false, volume);
            source.Play();

            float destroyDelay = Mathf.Max(0.01f, clip.length + 0.05f);
            DestroySource(source, destroyDelay);
        }

        public void PlayLoop(string loopId, string soundId, Vector3 position, float volume = 1f)
        {
            PlayLoop(loopId, ResolveClip(soundId), position, volume);
        }

        public void PlayLoop(string loopId, AudioClip clip, Vector3 position, float volume = 1f)
        {
            ValidateLoopId(loopId);
            ValidateClip(clip);
            loopId = loopId.Trim();

            if (loops.ContainsKey(loopId))
            {
                throw new InvalidOperationException(
                    $"Audio loop '{loopId}' is already playing. Stop it before starting another loop with the same id.");
            }

            AudioSource source = CreateSource($"Loop_{loopId}", position);
            ConfigureSource(source, clip, true, volume);
            loops.Add(loopId, new LoopInstance(source, Mathf.Clamp01(volume)));
            source.Play();
        }

        public bool IsLoopPlaying(string loopId)
        {
            ValidateLoopId(loopId);
            loopId = loopId.Trim();

            return loops.TryGetValue(loopId, out LoopInstance loop)
                && loop.Source != null
                && loop.Source.isPlaying;
        }

        public void StopLoop(string loopId)
        {
            ValidateLoopId(loopId);
            loopId = loopId.Trim();

            if (!loops.TryGetValue(loopId, out LoopInstance loop))
            {
                return;
            }

            loops.Remove(loopId);
            DestroySource(loop.Source);
        }

        public void StopAllLoops()
        {
            foreach (LoopInstance loop in loops.Values)
            {
                DestroySource(loop.Source);
            }

            loops.Clear();
        }

        private void RebuildClipLibrary()
        {
            clipById.Clear();

            if (clips == null)
            {
                return;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                ClipBinding binding = clips[i];

                if (binding == null || binding.Clip == null)
                {
                    continue;
                }

                AudioClip clip = binding.Clip;
                string clipId = string.IsNullOrWhiteSpace(binding.Id) ? clip.name : binding.Id.Trim();
                clipById[clipId] = clip;
            }
        }

        private AudioClip ResolveClip(string soundId)
        {
            if (string.IsNullOrWhiteSpace(soundId))
            {
                throw new ArgumentException("Sound id cannot be empty.", nameof(soundId));
            }

            if (clipById.TryGetValue(soundId.Trim(), out AudioClip clip))
            {
                return clip;
            }

            throw new KeyNotFoundException(
                $"Audio clip '{soundId}' is not registered in {nameof(AudioManager)}. "
                + "Add it to the Clips list or call the AudioClip overload directly.");
        }

        private AudioSource CreateSource(string sourceName, Vector3 position)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.position = position;
            sourceObject.transform.SetParent(transform, true);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = Mathf.Max(minDistance, maxDistance);
            source.rolloffMode = AudioRolloffMode.Logarithmic;

            return source;
        }

        private void ConfigureSource(AudioSource source, AudioClip clip, bool loop, float volume)
        {
            source.clip = clip;
            source.loop = loop;
            source.volume = CalculateRuntimeVolume(volume);
        }

        private float CalculateRuntimeVolume(float volume)
        {
            return isMuted ? 0f : Mathf.Clamp01(volume) * masterVolume;
        }

        private void RefreshLoopVolumes()
        {
            foreach (LoopInstance loop in loops.Values)
            {
                if (loop.Source != null)
                {
                    loop.Source.volume = CalculateRuntimeVolume(loop.BaseVolume);
                }
            }
        }

        private static void ValidateClip(AudioClip clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip), "Audio clip cannot be null.");
            }
        }

        private static void ValidateLoopId(string loopId)
        {
            if (string.IsNullOrWhiteSpace(loopId))
            {
                throw new ArgumentException("Loop id cannot be empty.", nameof(loopId));
            }
        }

        private static void DestroySource(AudioSource source)
        {
            DestroySource(source, 0f);
        }

        private static void DestroySource(AudioSource source, float delay)
        {
            if (source == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(source.gameObject, delay);
            }
            else
            {
                DestroyImmediate(source.gameObject);
            }
        }

        private void OnValidate()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            maxDistance = Mathf.Max(0.01f, maxDistance);
            minDistance = Mathf.Clamp(minDistance, 0f, maxDistance);
        }

        private void OnDestroy()
        {
            StopAllLoops();

            if (ServiceLocator.TryGet(out IAudioManager service) && ReferenceEquals(service, this))
            {
                ServiceLocator.Unregister<IAudioManager>(this);
            }
        }

        [Serializable]
        private sealed class ClipBinding
        {
            [SerializeField] private string id = string.Empty;
            [SerializeField] private AudioClip clip = null;

            public string Id => id;

            public AudioClip Clip => clip;
        }

        private sealed class LoopInstance
        {
            public LoopInstance(AudioSource source, float baseVolume)
            {
                Source = source;
                BaseVolume = baseVolume;
            }

            public AudioSource Source { get; }

            public float BaseVolume { get; }
        }
    }
}
