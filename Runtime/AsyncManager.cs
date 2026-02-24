using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class AsyncManager
{
    public int currentAsyncCount { get; private set; } = 0;

    public AsyncOperationHandle<T> LoadAsset<T>(AssetReference reference, Action<AsyncOperationHandle<T>> onComplete = null)
    {
        currentAsyncCount++;
        var handle = reference.LoadAssetAsync<T>();
        handle.Completed += op => {
            currentAsyncCount--;
            onComplete?.Invoke(op);
        };
        return handle;
    }

    public AsyncOperation LoadScene(string sceneName, Action<AsyncOperation> onComplete = null)
    {
        currentAsyncCount++;
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.completed += _ => {
            currentAsyncCount--;
            onComplete?.Invoke(op);
        };
        return op;
    }
}