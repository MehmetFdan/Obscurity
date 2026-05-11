using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace HorrorGame.Core
{
    /// <summary>
    /// Async scene loading contract backed by Addressables.
    /// </summary>
    public interface ISceneLoader : IService
    {
        bool IsLoading { get; }

        AsyncOperationHandle<SceneInstance> LoadSceneAsync(
            string sceneAddress,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100);

        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(
            SceneInstance sceneInstance,
            bool autoReleaseHandle = true);
    }
}
