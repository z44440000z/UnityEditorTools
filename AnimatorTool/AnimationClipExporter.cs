using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;
using System.Text;

public class AnimationClipExporter : EditorWindow
{
    private GameObject fbxAsset;
    private AnimatorController controller;
    private string exportFolder = "Assets/ExportedClips";

    [MenuItem("Tools/Animation/Clip 匯出工具")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AnimationClipExporter), false, "Clip 匯出工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("匯出 Animator Controller 使用的動畫 ", EditorStyles.boldLabel);

        fbxAsset = (GameObject)EditorGUILayout.ObjectField("FBX 模型", fbxAsset, typeof(GameObject), false);
        controller = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", controller, typeof(AnimatorController), false);

        EditorGUILayout.BeginHorizontal();
        GUIStyle wrapStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
        // 計算需要的高度
        float height = wrapStyle.CalcHeight(new GUIContent(exportFolder), EditorGUIUtility.currentViewWidth / 3);
        EditorGUILayout.LabelField("匯出路徑", exportFolder, wrapStyle, GUILayout.Height(height));
        if (GUILayout.Button("選擇資料夾", GUILayout.Width(120)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("選擇匯出資料夾", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    exportFolder = "Assets" + selectedPath.Substring(Application.dataPath.Length).Replace("\\", "/");
                }
                else
                {
                    Debug.LogError("請選擇專案內的 Assets 資料夾！");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("匯出"))
        {
            ExportClips();
        }
    }

    private void ExportClips()
    {
        if (fbxAsset == null || controller == null)
        {
            Debug.LogError("請選擇 FBX 模型 和 AnimatorController！");
            return;
        }

        // 讀取 FBX 的所有 Clip
        string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
        Object[] fbxAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

        Dictionary<string, AnimationClip> fbxClips = new Dictionary<string, AnimationClip>();
        foreach (Object asset in fbxAssets)
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                if (!fbxClips.ContainsKey(clip.name))
                {
                    fbxClips.Add(clip.name, clip);
                }
            }
        }

        // 確保 exportFolder 是合法的 Assets/... 路徑
        if (string.IsNullOrEmpty(exportFolder) || !exportFolder.StartsWith("Assets"))
        {
            Debug.LogError("Export folder 必須是 Assets 下的路徑，例如 Assets/ExportedClips");
            return;
        }

        EnsureFolderExists(exportFolder);

        HashSet<string> exported = new HashSet<string>();
        int count = 0;

        // 搜尋 Controller 中所有 Clip（包含 sub-state & blend tree）
        foreach (var layer in controller.layers)
        {
            FindClipsInStateMachine(layer.stateMachine, fbxClips, exported, ref count);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎉 匯出完成，共 {count} 個動畫 Clip。");
    }

    // 逐層建立資料夾（如果不存在）
    private void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        // 以 '/' 分割
        string[] parts = folderPath.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
        string cur = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(cur, parts[i]);
            }
            cur = next;
        }
    }

    private void FindClipsInStateMachine(AnimatorStateMachine stateMachine, Dictionary<string, AnimationClip> fbxClips, HashSet<string> exported, ref int count)
    {
        foreach (var state in stateMachine.states)
        {
            if (state.state.motion != null)
            {
                ExportMotion(state.state.motion, fbxClips, exported, ref count);
            }
        }

        foreach (var subState in stateMachine.stateMachines)
        {
            FindClipsInStateMachine(subState.stateMachine, fbxClips, exported, ref count);
        }
    }

    private void ExportMotion(Motion motion, Dictionary<string, AnimationClip> fbxClips, HashSet<string> exported, ref int count)
    {
        if (motion is AnimationClip clip)
        {
            if (clip == null) return;

            string clipName = clip.name;
            if (fbxClips.ContainsKey(clipName) && !exported.Contains(clipName))
            {
                string safeName = SanitizeFileName(clipName);
                string newPath = $"{exportFolder}/{safeName}.anim";

                // 若檔案已存在，自動加序號
                newPath = GetUniqueAssetPath(newPath);

                Debug.Log($"Creating asset at: {newPath} (original clip name: '{clipName}')");

                try
                {
                    AnimationClip newClip = Object.Instantiate(fbxClips[clipName]);
                    AssetDatabase.CreateAsset(newClip, newPath);
                    exported.Add(clipName);
                    count++;
                    Debug.Log($"✅ 匯出動畫: {newPath}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"匯出失敗: {newPath}  Exception: {ex.Message}\nStack: {ex.StackTrace}");
                }
            }
        }
        else if (motion is BlendTree blendTree)
        {
            foreach (var child in blendTree.children)
            {
                if (child.motion != null)
                {
                    ExportMotion(child.motion, fbxClips, exported, ref count);
                }
            }
        }
    }

    // 把不合法檔名字元替換成 '_'
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (char c in name)
        {
            bool isInvalid = false;
            for (int i = 0; i < invalid.Length; i++)
            {
                if (c == invalid[i]) { isInvalid = true; break; }
            }
            sb.Append(isInvalid ? '_' : c);
        }
        // 再把空白開頭/結尾 trim 掉
        string s = sb.ToString().Trim();
        if (string.IsNullOrEmpty(s)) s = "anim_clip";
        return s;
    }

    // 如果 newPath 已存在，加入序號避免衝突
    private static string GetUniqueAssetPath(string path)
    {
        if (!File.Exists(path) && !AssetDatabase.LoadAssetAtPath<Object>(path)) return path;

        string dir = Path.GetDirectoryName(path).Replace("\\", "/");
        string filename = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        int idx = 1;
        string candidate;
        do
        {
            candidate = $"{dir}/{filename}_{idx}{ext}";
            idx++;
        } while (File.Exists(candidate) || AssetDatabase.LoadAssetAtPath<Object>(candidate) != null);

        return candidate;
    }
}
