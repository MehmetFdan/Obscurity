using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace HorrorGame.Core
{
    /// <summary>
    /// Raised immediately before an Addressables scene load begins.
    /// </summary>
    public readonly struct SceneLoadStartedEvent
    {
        public SceneLoadStartedEvent(string sceneAddress, LoadSceneMode loadSceneMode)
        {
            SceneAddress = sceneAddress;
            LoadSceneMode = loadSceneMode;
        }

        public string SceneAddress { get; }

        public LoadSceneMode LoadSceneMode { get; }
    }

    /// <summary>
    /// Raised when an Addressables scene load operation completes.
    /// </summary>
    public readonly struct SceneLoadCompletedEvent
    {
        public SceneLoadCompletedEvent(string sceneAddress, AsyncOperationStatus status)
        {
            SceneAddress = sceneAddress;
            Status = status;
        }

        public string SceneAddress { get; }

        public AsyncOperationStatus Status { get; }

        public bool Succeeded => Status == AsyncOperationStatus.Succeeded;
    }

    /// <summary>
    /// Raised when an Addressables scene unload operation completes.
    /// </summary>
    public readonly struct SceneUnloadCompletedEvent
    {
        public SceneUnloadCompletedEvent(string sceneName, AsyncOperationStatus status)
        {
            SceneName = sceneName;
            Status = status;
        }

        public string SceneName { get; }

        public AsyncOperationStatus Status { get; }

        public bool Succeeded => Status == AsyncOperationStatus.Succeeded;
    }
}
