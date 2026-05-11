using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Central registry for scene-level and application-level services.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new();
        private static readonly object SyncRoot = new();

        /// <summary>
        /// Registers a service against its public contract.
        /// </summary>
        public static void Register<TService>(TService service, bool overwrite = false)
            where TService : class, IService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            lock (SyncRoot)
            {
                Type serviceType = typeof(TService);

                if (Services.TryGetValue(serviceType, out IService existingService)
                    && !ReferenceEquals(existingService, service))
                {
                    if (!overwrite)
                    {
                        throw new InvalidOperationException(
                            $"Service '{serviceType.Name}' is already registered.");
                    }
                }

                Services[serviceType] = service;
            }
        }

        /// <summary>
        /// Resolves a required service or throws when it is missing.
        /// </summary>
        public static TService Get<TService>()
            where TService : class, IService
        {
            if (TryGet(out TService service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"Service '{typeof(TService).Name}' is not registered.");
        }

        /// <summary>
        /// Attempts to resolve a service without throwing.
        /// </summary>
        public static bool TryGet<TService>(out TService service)
            where TService : class, IService
        {
            lock (SyncRoot)
            {
                if (Services.TryGetValue(typeof(TService), out IService registeredService)
                    && registeredService is TService typedService)
                {
                    service = typedService;
                    return true;
                }
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Returns true when a service contract already has a registered instance.
        /// </summary>
        public static bool IsRegistered<TService>()
            where TService : class, IService
        {
            lock (SyncRoot)
            {
                return Services.ContainsKey(typeof(TService));
            }
        }

        /// <summary>
        /// Removes a service registration when the owner is destroyed.
        /// </summary>
        public static void Unregister<TService>(TService service = null)
            where TService : class, IService
        {
            lock (SyncRoot)
            {
                Type serviceType = typeof(TService);

                if (!Services.TryGetValue(serviceType, out IService registeredService))
                {
                    return;
                }

                if (service != null && !ReferenceEquals(registeredService, service))
                {
                    return;
                }

                Services.Remove(serviceType);
            }
        }

        /// <summary>
        /// Clears all registrations. Intended for tests and Unity play-mode reloads.
        /// </summary>
        public static void Clear()
        {
            lock (SyncRoot)
            {
                Services.Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            Clear();
        }
    }
}
