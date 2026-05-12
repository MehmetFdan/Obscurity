using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HorrorGame.Core
{
    /// <summary>
    /// Addressables-backed scene loader registered through the ServiceLocator.
    /// </summary>
    [DefaultExecutionOrder(ServiceExecutionOrder.CoreServices)]
    [DisallowMultipleComponent]
    public sealed class AddressableSceneLoader : MonoBehaviour, ISceneLoader
    {
        [SerializeField] private bool registerOnAwake = true;
        [SerializeField] private bool dontDestroyOnLoad = true;

        public bool IsLoading { get; private set; }

        public AsyncOperationHandle<SceneInstance> CurrentOperation { get; private set; }

        private void Awake()
        {
            if (dontDestroyOnLoad && Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (registerOnAwake)
            {
                ServiceLocator.Register<ISceneLoader>(this);
            }
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(
            string sceneAddress,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100)
        {
            if (string.IsNullOrWhiteSpace(sceneAddress))
            {
                throw new ArgumentException("Scene address cannot be empty.", nameof(sceneAddress));
            }

            EnsureNotLoading();

            IsLoading = true;
            EventBus<SceneLoadStartedEvent>.Publish(new SceneLoadStartedEvent(sceneAddress, loadSceneMode));

            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                sceneAddress,
                loadSceneMode,
                activateOnLoad,
                priority);

            CurrentOperation = handle;
            handle.Completed += completedHandle => CompleteLoad(sceneAddress, completedHandle.Status);

            return handle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(
            SceneInstance sceneInstance,
            bool autoReleaseHandle = true)
        {
            EnsureNotLoading();

            IsLoading = true;
            string sceneName = sceneInstance.Scene.IsValid() ? sceneInstance.Scene.name : string.Empty;
            AsyncOperationHandle<SceneInstance> handle = Addressables.UnloadSceneAsync(
                sceneInstance,
                autoReleaseHandle);

            handle.Completed += completedHandle => CompleteUnload(sceneName, completedHandle.Status);

            return handle;
        }

        private void CompleteLoad(string sceneAddress, AsyncOperationStatus status)
        {
            IsLoading = false;
            EventBus<SceneLoadCompletedEvent>.Publish(new SceneLoadCompletedEvent(sceneAddress, status));
        }

        private void CompleteUnload(string sceneName, AsyncOperationStatus status)
        {
            IsLoading = false;
            EventBus<SceneUnloadCompletedEvent>.Publish(new SceneUnloadCompletedEvent(sceneName, status));
        }

        private void EnsureNotLoading()
        {
            if (IsLoading)
            {
                throw new InvalidOperationException("A scene load operation is already in progress.");
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out ISceneLoader service) && ReferenceEquals(service, this))
            {
                ServiceLocator.Unregister<ISceneLoader>(this);
            }
        }
    }
}
