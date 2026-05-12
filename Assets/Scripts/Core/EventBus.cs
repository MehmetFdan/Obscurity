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
        public const int DefaultListenerLimitPerEvent = 32;

        private static readonly HashSet<Action> ClearActions = new();

        /// <summary>
        /// Enables lightweight publish logs in editor/development sessions.
        /// Keep disabled during normal play because high-frequency events can be noisy.
        /// </summary>
        public static bool DebugLoggingEnabled { get; set; }

        internal static void RegisterClearAction(Action clearAction)
        {
            ClearActions.Add(clearAction);
        }

        internal static void LogPublish(Type eventType, int listenerCount)
        {
            if (!DebugLoggingEnabled || (!Application.isEditor && !Debug.isDebugBuild))
            {
                return;
            }

            Debug.Log($"EventBus<{eventType.Name}> published to {listenerCount} listener(s).");
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
        private static int listenerCount;
        private static int listenerLimit = EventBus.DefaultListenerLimitPerEvent;

        static EventBus()
        {
            EventBus.RegisterClearAction(Clear);
        }

        /// <summary>
        /// Number of active listeners on this event channel.
        /// </summary>
        public static int ListenerCount => listenerCount;

        /// <summary>
        /// Maximum listeners allowed on this event channel before subscription is rejected.
        /// </summary>
        public static int ListenerLimit
        {
            get => listenerLimit;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        "EventBus listener limit must be greater than zero.");
                }

                listenerLimit = value;
            }
        }

        /// <summary>
        /// Returns true when this channel has at least one listener.
        /// </summary>
        public static bool HasListeners()
        {
            return listenerCount > 0;
        }

        /// <summary>
        /// Registers a listener. Prefer calling from OnEnable for MonoBehaviours.
        /// </summary>
        public static void Subscribe(Action<TEvent> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (listenerCount >= listenerLimit)
            {
                throw new InvalidOperationException(
                    $"EventBus<{typeof(TEvent).Name}> listener limit exceeded. "
                    + $"Limit: {listenerLimit}. Current: {listenerCount}. "
                    + "Use direct references for one-to-one communication or split the event into narrower channels.");
            }

            Event += listener;
            listenerCount++;
        }

        /// <summary>
        /// Removes a listener. Prefer calling from OnDisable for MonoBehaviours.
        /// </summary>
        public static void Unsubscribe(Action<TEvent> listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (!ContainsListener(listener))
            {
                return;
            }

            Event -= listener;
            listenerCount = Math.Max(0, listenerCount - 1);
        }

        /// <summary>
        /// Publishes an event to all current listeners.
        /// </summary>
        public static void Publish(TEvent eventData)
        {
            Event?.Invoke(eventData);
            EventBus.LogPublish(typeof(TEvent), listenerCount);
        }

        /// <summary>
        /// Clears this event channel.
        /// </summary>
        public static void Clear()
        {
            Event = null;
            listenerCount = 0;
        }

        private static bool ContainsListener(Action<TEvent> listener)
        {
            if (Event == null)
            {
                return false;
            }

            foreach (Delegate existingListener in Event.GetInvocationList())
            {
                if (Equals(existingListener, listener))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
