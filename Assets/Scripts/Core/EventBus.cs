using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Runtime utility for clearing all generic event bus channels.
    /// </summary>
    public static class EventBus
    {
        private static readonly HashSet<Action> ClearActions = new();

        internal static void RegisterClearAction(Action clearAction)
        {
            ClearActions.Add(clearAction);
        }

        /// <summary>
        /// Clears every event channel that has been used in the current domain.
        /// </summary>
        public static void ClearAll()
        {
            foreach (Action clearAction in ClearActions)
            {
                clearAction.Invoke();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            ClearAll();
        }
    }

    /// <summary>
    /// Lightweight static event channel for struct-based gameplay events.
    /// </summary>
    public static class EventBus<TEvent>
        where TEvent : struct
    {
        private static Action<TEvent> Event;

        static EventBus()
        {
            EventBus.RegisterClearAction(Clear);
        }

        /// <summary>
        /// Number of active listeners on this event channel.
        /// </summary>
        public static int ListenerCount => Event?.GetInvocationList().Length ?? 0;

        /// <summary>
        /// Registers a listener. Prefer calling from OnEnable for MonoBehaviours.
        /// </summary>
        public static void Subscribe(Action<TEvent> listener)
        {
            Event += listener ?? throw new ArgumentNullException(nameof(listener));
        }

        /// <summary>
        /// Removes a listener. Prefer calling from OnDisable for MonoBehaviours.
        /// </summary>
        public static void Unsubscribe(Action<TEvent> listener)
        {
            Event -= listener ?? throw new ArgumentNullException(nameof(listener));
        }

        /// <summary>
        /// Publishes an event to all current listeners.
        /// </summary>
        public static void Publish(TEvent eventData)
        {
            Event?.Invoke(eventData);
        }

        /// <summary>
        /// Clears this event channel.
        /// </summary>
        public static void Clear()
        {
            Event = null;
        }
    }
}
