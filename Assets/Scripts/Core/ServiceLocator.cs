using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HorrorGame.Core
{
    /// <summary>
    /// Central registry for scene-level and application-level services.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IService> Services = new();

        /// <summary>
        /// Registers a service against its public contract.
        /// </summary>
        public static void Register<TService>(TService service, bool overwrite = false)
            where TService : class, IService
        {
            Type serviceType = GetValidatedContract<TService>();

            if (service == null)
            {
                throw new ArgumentNullException(
                    nameof(service),
                    $"Cannot register a null service for '{serviceType.Name}'.");
            }

            if (Services.TryGetValue(serviceType, out IService existingService)
                && !ReferenceEquals(existingService, service))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException(
                        $"Service '{serviceType.Name}' is already registered by "
                        + $"'{existingService.GetType().Name}'. Pass overwrite: true to replace it with "
                        + $"'{service.GetType().Name}'.");
                }
            }

            Services[serviceType] = service;
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
                $"Service '{typeof(TService).Name}' is not registered. Register it in Awake() "
                + $"with ServiceLocator.Register<{typeof(TService).Name}>(implementation) before "
                + $"resolving it in Start(). Registered services: {GetRegisteredServicesSummary()}");
        }

        /// <summary>
        /// Attempts to resolve a service without throwing.
        /// </summary>
        public static bool TryGet<TService>(out TService service)
            where TService : class, IService
        {
            Type serviceType = GetValidatedContract<TService>();

            if (Services.TryGetValue(serviceType, out IService registeredService)
                && registeredService is TService typedService)
            {
                service = typedService;
                return true;
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
            return Services.ContainsKey(GetValidatedContract<TService>());
        }

        /// <summary>
        /// Removes a service registration when the owner is destroyed.
        /// </summary>
        public static bool Unregister<TService>(TService service = null)
            where TService : class, IService
        {
            Type serviceType = GetValidatedContract<TService>();

            if (!Services.TryGetValue(serviceType, out IService registeredService))
            {
                return false;
            }

            if (service != null && !ReferenceEquals(registeredService, service))
            {
                return false;
            }

            return Services.Remove(serviceType);
        }

        /// <summary>
        /// Clears all registrations. Intended for tests and Unity play-mode reloads.
        /// </summary>
        public static void Clear()
        {
            Services.Clear();
        }

#if UNITY_EDITOR
        private const string EditorMenuRoot = "FPS Horror Game/Services/";

        [MenuItem(EditorMenuRoot + "List Registered Services")]
        private static void ListRegisteredServicesMenu()
        {
            Debug.Log(GetRegisteredServicesDebugText());
        }

        /// <summary>
        /// Returns a snapshot of registered services for editor diagnostics.
        /// </summary>
        public static IReadOnlyList<ServiceRegistration> GetRegisteredServices()
        {
            var registrations = new List<ServiceRegistration>(Services.Count);

            foreach (KeyValuePair<Type, IService> service in Services)
            {
                registrations.Add(new ServiceRegistration(service.Key, service.Value));
            }

            return registrations;
        }

        /// <summary>
        /// Returns a readable service list for editor windows, logs, and debug UI.
        /// </summary>
        public static string GetRegisteredServicesDebugText()
        {
            if (Services.Count == 0)
            {
                return "ServiceLocator: no registered services.";
            }

            var builder = new StringBuilder("ServiceLocator registered services:");

            foreach (KeyValuePair<Type, IService> service in Services)
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(service.Key.Name);
                builder.Append(" -> ");
                builder.Append(service.Value.GetType().Name);
            }

            return builder.ToString();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
        {
            Clear();
        }

        private static Type GetValidatedContract<TService>()
            where TService : class, IService
        {
            Type serviceType = typeof(TService);

            if (!serviceType.IsInterface)
            {
                throw new InvalidOperationException(
                    $"Service contract '{serviceType.Name}' must be an interface. "
                    + "Register services behind interfaces, for example "
                    + "ServiceLocator.Register<IMyService>(implementation).");
            }

            return serviceType;
        }

        private static string GetRegisteredServicesSummary()
        {
            if (Services.Count == 0)
            {
                return "none";
            }

            var builder = new StringBuilder();

            foreach (KeyValuePair<Type, IService> service in Services)
            {
                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(service.Key.Name);
                builder.Append(" -> ");
                builder.Append(service.Value.GetType().Name);
            }

            return builder.ToString();
        }

#if UNITY_EDITOR
        public readonly struct ServiceRegistration
        {
            public ServiceRegistration(Type contractType, IService instance)
            {
                ContractType = contractType;
                ImplementationType = instance.GetType();
                Instance = instance;
            }

            public Type ContractType { get; }

            public Type ImplementationType { get; }

            public IService Instance { get; }

            public string DisplayName => $"{ContractType.Name} -> {ImplementationType.Name}";
        }
#endif
    }

    /// <summary>
    /// Shared execution-order slots for ServiceLocator providers and consumers.
    /// </summary>
    public static class ServiceExecutionOrder
    {
        public const int CoreServices = -1000;
        public const int FeatureServices = -900;
        public const int PlayerServices = -800;
        public const int ServiceConsumers = 0;
    }
}
