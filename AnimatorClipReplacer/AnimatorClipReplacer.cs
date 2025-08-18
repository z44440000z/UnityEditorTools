using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animator Clip 替換工具
/// 此工具用來替換 Animator Controller 中的 Animation Clips，
/// 以便快速更新動畫資源，特別是當 FBX 動畫前綴發生變化時。
/// 用戶可以指定新的 FBX 資源，並自定義原始和新動畫的前綴。
/// </summary>
public class AnimatorClipReplacer : EditorWindow
{
    private AnimatorController animatorController;
    private GameObject newFbx;
    private string oldPrefix = "Arm_Collie|";
    private string newPrefix = "Arm_Shiba|";

    [MenuItem("Tools/替換工具/Animator Clip 替換")]
    static void ShowWindow()
    {
        GetWindow<AnimatorClipReplacer>("Animator Clip 替換");
    }

    void OnGUI()
    {
        // 設置區塊樣式
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.normal.textColor = Color.white;
        boxStyle.normal.background = Texture2D.grayTexture;


        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("Animator Clip 替換工具", EditorStyles.boldLabel);
        GUILayout.Space(50); // 空隙
        GUILayout.EndVertical();

        animatorController = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", animatorController, typeof(AnimatorController), false);
        newFbx = (GameObject)EditorGUILayout.ObjectField("新動畫 FBX", newFbx, typeof(GameObject), false);

        oldPrefix = EditorGUILayout.TextField("原始動畫前綴", oldPrefix);
        newPrefix = EditorGUILayout.TextField("新動畫前綴", newPrefix);

        if (GUILayout.Button("執行替換"))
        {
            ReplaceClips();
        }
    }

    void ReplaceClips()
    {
        if (animatorController == null || newFbx == null)
        {
            Debug.LogError("請指定 Animator Controller 與 FBX");
            return;
        }

        // 從 FBX 中獲取 AnimationClips
        var clips = new List<AnimationClip>();
        var animationClips = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(newFbx)).OfType<AnimationClip>().ToArray();

        // 使用字典存儲新的動畫 clip，並使用新的前綴命名
        var newClips = new Dictionary<string, AnimationClip>();
        foreach (var clip in animationClips)
        {
            string nameWithoutPrefix = clip.name.StartsWith(newPrefix) ? clip.name.Substring(newPrefix.Length) : clip.name;
            newClips[nameWithoutPrefix] = clip;
        }

        Undo.RegisterCompleteObjectUndo(animatorController, "Replace Animator Clips");

        int replacedCount = 0;

        foreach (var layer in animatorController.layers)
        {
            replacedCount += ReplaceInStateMachine(layer.stateMachine, newClips, oldPrefix);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ 替換完成，共替換 {replacedCount} 個動畫");
    }

    int ReplaceInStateMachine(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> newClips, string oldPrefix)
    {
        int localReplaced = 0;

        foreach (var state in stateMachine.states)
        {
            if (state.state.motion is AnimationClip clip)
            {
                string baseName = clip.name.StartsWith(oldPrefix) ? clip.name.Substring(oldPrefix.Length) : clip.name;

                if (newClips.TryGetValue(baseName, out var newClip))
                {
                    Undo.RecordObject(state.state, "Replace Animation Clip");
                    state.state.motion = newClip;
                    localReplaced++;
                    Debug.Log($"🎞 替換動畫: {clip.name} ➜ {newClip.name}");
                }
            }
            else if (state.state.motion is BlendTree blendTree)
            {
                localReplaced += ReplaceInBlendTree(blendTree, newClips, oldPrefix);
            }
        }

        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            Undo.RegisterCompleteObjectUndo(subStateMachine.stateMachine, "Replace SubStateMachine");
            localReplaced += ReplaceInStateMachine(subStateMachine.stateMachine, newClips, oldPrefix);
        }

        return localReplaced;
    }

    int ReplaceInBlendTree(BlendTree blendTree, Dictionary<string, AnimationClip> newClips, string oldPrefix)
    {
        int localReplaced = 0;

        Undo.RecordObject(blendTree, "Replace BlendTree Motions");

        for (int i = 0; i < blendTree.children.Length; i++)
        {
            var child = blendTree.children[i];

            if (child.motion is AnimationClip clip)
            {
                string baseName = clip.name.StartsWith(oldPrefix) ? clip.name.Substring(oldPrefix.Length) : clip.name;

                if (newClips.TryGetValue(baseName, out var newClip))
                {
                    var children = blendTree.children;
                    children[i].motion = newClip;
                    blendTree.children = children;
                    localReplaced++;
                    Debug.Log($"🔁 替換 BlendTree 動畫: {clip.name} ➜ {newClip.name}");
                }
            }
            else if (child.motion is BlendTree nestedTree)
            {
                localReplaced += ReplaceInBlendTree(nestedTree, newClips, oldPrefix);
            }
        }

        return localReplaced;
    }
}
