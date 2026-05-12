using HorrorGame.Core;
using UnityEngine;

namespace HorrorGame.Audio
{
    /// <summary>
    /// Example consumer: resolves the audio service in Start after all Awake registrations are complete.
    /// </summary>
    [DefaultExecutionOrder(ServiceExecutionOrder.ServiceConsumers)]
    public sealed class AudioServiceUsageExample : MonoBehaviour
    {
        [SerializeField] private string startSoundId = "door_creak";
        [SerializeField] private string ambientLoopId = "main_ambient";
        [SerializeField] private string ambientSoundId = "basement_ambience";
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        private IAudioManager audioManager;

        private void Start()
        {
            audioManager = ServiceLocator.Get<IAudioManager>();
            audioManager.PlaySFX(startSoundId, transform.position, volume);
            audioManager.PlayLoop(ambientLoopId, ambientSoundId, transform.position, volume);
        }

        private void OnDestroy()
        {
            if (audioManager != null && audioManager.IsLoopPlaying(ambientLoopId))
            {
                audioManager.StopLoop(ambientLoopId);
            }
        }
    }
}
