using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
[InitializeOnLoad]
/// <summary>
/// 監聽 Build Path 的變更，並自動更新 Addressables Profile 中的 WEB_OUTPUT_DIR
/// 這樣可以確保在每次 Build 後，Addressables 都能正確指向新的輸出路徑
/// </summary>
public static class BuildPathWatcher
{
    static string lastBuildPath;

    static BuildPathWatcher()
    {
        // 初始值設為目前的 Build Path
        lastBuildPath = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget);

        // 每幀監聽
        EditorApplication.update += OnEditorUpdate;
    }

    [MenuItem("Addressables 設定/更新 Addressables Profile")]
    static void OnEditorUpdate()
    {
        // 取得當前 Build 輸出路徑
        string currentPath = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.activeBuildTarget);

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        string currentExeOutputDir = settings.profileSettings.GetValueByName(settings.activeProfileId, "WEB_OUTPUT_DIR");

        // 若有變更，並且不是空字串
        if (!string.IsNullOrEmpty(currentPath) && currentPath != currentExeOutputDir)
        {
            lastBuildPath = currentPath;
            UpdateAddressablesProfile(currentPath);
        }
    }

    static void UpdateAddressablesProfile(string exePath)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogWarning("[BuildPathWatcher] 找不到 Addressables 設定，請先打開 Addressables Groups 視窗初始化！");
            return;
        }

        string profileId = settings.activeProfileId;
        settings.profileSettings.SetValue(profileId, "WEB_OUTPUT_DIR", exePath);

        Debug.Log($"[BuildPathWatcher] 已自動更新 Addressables Profile 中的 WEB_OUTPUT_DIR = {exePath}");
    }
}
