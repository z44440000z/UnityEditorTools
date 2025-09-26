using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animator Clip 替換工具（同名 / 前綴）
/// - 同名替換分為「搜尋」與「替換」兩步
/// - 只替換來自舊 FBX 的動畫，本來就屬於新 FBX 的動畫不會動
/// </summary>
public class AnimatorClipReplacer_v2 : EditorWindow
{
    private AnimatorController animatorController;

    private enum ReplaceMode { 同名替換, 前綴替換 }
    private ReplaceMode mode = ReplaceMode.同名替換;

    // 同名替換
    private GameObject oldFbx;
    private GameObject newFbx;
    private HashSet<string> clipsNeedingReplace = new HashSet<string>(); // 搜尋結果快取
    private List<AnimationClip> foundClips = new List<AnimationClip>();
    private Vector2 scroll;

    // 前綴替換
    private string oldPrefix = "Arm_Collie|";
    private string newPrefix = "Arm_Shiba|";
    private GameObject prefixNewFbx;

    [MenuItem("Tools/替換工具/Animator Clip 替換 (進階)")]
    static void ShowWindow()
    {
        GetWindow<AnimatorClipReplacer_v2>("Animator Clip 替換");
    }

    void OnGUI()
    {
        GUILayout.Label("Animator Clip 替換工具", EditorStyles.boldLabel);

        animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
        mode = (ReplaceMode)EditorGUILayout.EnumPopup("替換模式", mode);

        GUILayout.Space(10);

        if (mode == ReplaceMode.同名替換)
        {
            oldFbx = (GameObject)EditorGUILayout.ObjectField("舊模型 FBX", oldFbx, typeof(GameObject), false);
            newFbx = (GameObject)EditorGUILayout.ObjectField("新模型 FBX", newFbx, typeof(GameObject), false);

            GUILayout.Space(10);

GUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 搜尋可替換動畫"))
            {
                SearchClipsNeedingReplace();
            }

                            if (GUILayout.Button("✅ 執行替換"))
                {
                    ReplaceClipsByName();
                }
            GUILayout.EndHorizontal();

            if (foundClips.Count > 0)
            {
                scroll = GUILayout.BeginScrollView(scroll);

                if (foundClips.Count == 0)
                {
                    GUILayout.Label("沒有找到相關動畫。");
                }
                else
                {
                    GUILayout.Label($"找到 {foundClips.Count} 個動畫：", EditorStyles.boldLabel);
                    foreach (var clip in foundClips)
                    {
                        EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                    }
                }

                GUILayout.EndScrollView();


            }
        }
        else if (mode == ReplaceMode.前綴替換)
        {
            oldPrefix = EditorGUILayout.TextField("原始前綴", oldPrefix);
            newPrefix = EditorGUILayout.TextField("新前綴", newPrefix);
            prefixNewFbx = (GameObject)EditorGUILayout.ObjectField("新模型 FBX", prefixNewFbx, typeof(GameObject), false);

            if (GUILayout.Button("執行前綴替換"))
            {
                ReplaceClipsByPrefix();
            }
        }
    }

    #region 模式一：同名替換
    void SearchClipsNeedingReplace()
    {
        foundClips.Clear();
        if (animatorController == null || oldFbx == null) return;

        // 取得舊模型資產內的所有動畫
        var modelAnimations = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(oldFbx));
        HashSet<AnimationClip> modelClips = new HashSet<AnimationClip>();
        foreach (var obj in modelAnimations)
        {
            if (obj is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                modelClips.Add(clip);
            }
        }

        // 比對 Animator 裡所有動畫
        foreach (var clip in animatorController.animationClips)
        {
            if (modelClips.Contains(clip))
            {
                foundClips.Add(clip);
            }
        }

        Debug.Log($"🔍 搜尋完成，共找到 {foundClips.Count} 個待替換動畫");
    }

    void SearchInStateMachine(AnimatorStateMachine stateMachine, HashSet<string> oldClipNames, HashSet<string> newClipNames)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                if (oldClipNames.Contains(clip.name)) // 來自舊模型
                {
                    clipsNeedingReplace.Add(clip.name);
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                SearchInBlendTree(blendTree, oldClipNames, newClipNames);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            SearchInStateMachine(subStateMachine.stateMachine, oldClipNames, newClipNames);
        }
    }

    void SearchInBlendTree(BlendTree blendTree, HashSet<string> oldClipNames, HashSet<string> newClipNames)
    {
        foreach (var child in blendTree.children)
        {
            if (child.motion is AnimationClip clip && oldClipNames.Contains(clip.name))
            {
                clipsNeedingReplace.Add(clip.name);
            }
            else if (child.motion is BlendTree nestedTree)
            {
                SearchInBlendTree(nestedTree, oldClipNames, newClipNames);
            }
        }
    }

    void ReplaceClipsByName()
    {
        if (foundClips.Count == 0)
        {
            Debug.LogWarning("⚠ 沒有可替換的動畫，請先執行搜尋！");
            return;
        }

        var newClips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(newFbx))
                                    .OfType<AnimationClip>()
                                    .ToDictionary(c => c.name, c => c);

        int replacedCount = 0;

        foreach (var layer in animatorController.layers)
        {
            replacedCount += ReplaceInStateMachine(layer.stateMachine, newClips);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 替換完成，共替換 {replacedCount} 個動畫");
    }

    int ReplaceInStateMachine(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> newClipDict)
    {
        int localReplaced = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip && foundClips.Contains(clip))
            {
                if (newClipDict.TryGetValue(clip.name, out var newClip))
                {
                    state.state.motion = newClip;
                    localReplaced++;
                    Debug.Log($"🎞 替換動畫: {clip.name} ➜ {newClip.name}");
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                localReplaced += ReplaceInBlendTree(blendTree, newClipDict);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            localReplaced += ReplaceInStateMachine(subStateMachine.stateMachine, newClipDict);
        }

        return localReplaced;
    }

    int ReplaceInBlendTree(BlendTree blendTree, Dictionary<string, AnimationClip> newClipDict)
    {
        int localReplaced = 0;
        var children = blendTree.children;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].motion is AnimationClip clip && foundClips.Contains(clip))
            {
                if (newClipDict.TryGetValue(clip.name, out var newClip))
                {
                    children[i].motion = newClip;
                    localReplaced++;
                    Debug.Log($"🔁 替換 BlendTree 動畫: {clip.name} ➜ {newClip.name}");
                }
            }
            else if (children[i].motion is BlendTree nestedTree)
            {
                localReplaced += ReplaceInBlendTree(nestedTree, newClipDict);
            }
        }

        blendTree.children = children;
        return localReplaced;
    }
    #endregion

    #region 模式二：前綴替換
    void ReplaceClipsByPrefix()
    {
        if (animatorController == null || prefixNewFbx == null)
        {
            Debug.LogError("請指定 Animator Controller 與 FBX");
            return;
        }

        var animationClips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(prefixNewFbx)).OfType<AnimationClip>().ToArray();
        var newClips = new Dictionary<string, AnimationClip>();

        foreach (var clip in animationClips)
        {
            string nameWithoutPrefix = clip.name.StartsWith(newPrefix) ? clip.name.Substring(newPrefix.Length) : clip.name;
            newClips[nameWithoutPrefix] = clip;
        }

        int replacedCount = 0;

        foreach (var layer in animatorController.layers)
        {
            replacedCount += ReplaceInStateMachinePrefix(layer.stateMachine, newClips, oldPrefix);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 前綴替換完成，共替換 {replacedCount} 個動畫");
    }

    int ReplaceInStateMachinePrefix(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> newClips, string oldPrefix)
    {
        int localReplaced = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                string baseName = clip.name.StartsWith(oldPrefix) ? clip.name.Substring(oldPrefix.Length) : clip.name;
                if (newClips.TryGetValue(baseName, out var newClip))
                {
                    state.state.motion = newClip;
                    localReplaced++;
                    Debug.Log($"🎞 替換動畫: {clip.name} ➜ {newClip.name}");
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                localReplaced += ReplaceInBlendTreePrefix(blendTree, newClips, oldPrefix);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            localReplaced += ReplaceInStateMachinePrefix(subStateMachine.stateMachine, newClips, oldPrefix);
        }

        return localReplaced;
    }

    int ReplaceInBlendTreePrefix(BlendTree blendTree, Dictionary<string, AnimationClip> newClips, string oldPrefix)
    {
        int localReplaced = 0;
        var children = blendTree.children;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].motion is AnimationClip clip)
            {
                string baseName = clip.name.StartsWith(oldPrefix) ? clip.name.Substring(oldPrefix.Length) : clip.name;
                if (newClips.TryGetValue(baseName, out var newClip))
                {
                    children[i].motion = newClip;
                    localReplaced++;
                    Debug.Log($"🔁 替換 BlendTree 動畫: {clip.name} ➜ {newClip.name}");
                }
            }
            else if (children[i].motion is BlendTree nestedTree)
            {
                localReplaced += ReplaceInBlendTreePrefix(nestedTree, newClips, oldPrefix);
            }
        }

        blendTree.children = children;
        return localReplaced;
    }
    #endregion
}
