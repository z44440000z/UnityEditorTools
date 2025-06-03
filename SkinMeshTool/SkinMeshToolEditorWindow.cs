using UnityEditor;
using UnityEngine;

public class SkinMeshToolEditorWindow : EditorWindow
{
    private SkinnedMeshRenderer originalSkinnedMesh;
    private SkinnedMeshRenderer newSkinnedMesh;

    string originalSkinnedMeshName;
    int originalSkinnedMeshBoneCount;
    string newSkinnedMeshName;
    int newSkinnedMeshBoneCount;

    [MenuItem("Tools/替換工具/骨架替換工具")]
    public static void ShowWindow()
    {
        GetWindow<SkinMeshToolEditorWindow>("骨架替換工具");
    }

    private void OnGUI()
    {
        // 設置區塊樣式
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.normal.textColor = Color.white;
        boxStyle.normal.background = Texture2D.grayTexture;

        // 開始垂直佈局
        EditorGUILayout.BeginVertical(boxStyle);
        // 在區塊內顯示文字
        GUILayout.Label("💀 骨架替換工具", EditorStyles.whiteLabel);
        GUILayout.Space(50); // 空隙
        GUILayout.EndVertical();

        EditorGUIUtility.labelWidth = 180; // 👉 可以根據需求調整
        originalSkinnedMesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Original Skinned Mesh (身體)", originalSkinnedMesh, typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
        newSkinnedMesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("New Skinned Mesh (配件)", newSkinnedMesh, typeof(SkinnedMeshRenderer), true, GUILayout.ExpandWidth(true));
        EditorGUIUtility.labelWidth = 150;
        EditorGUILayout.Space();
        if (originalSkinnedMesh != null)
        {
            originalSkinnedMeshName = originalSkinnedMesh.name;
            originalSkinnedMeshBoneCount = originalSkinnedMesh.bones.Length;
        }
        else
        {
            originalSkinnedMeshName = "未指定";
            originalSkinnedMeshBoneCount = 0;
        }
        EditorGUILayout.LabelField($"身體骨架名稱：{originalSkinnedMeshName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"身體骨頭數量：{originalSkinnedMeshBoneCount}", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        if (newSkinnedMesh != null)
        {
            newSkinnedMeshName = newSkinnedMesh.name;
            newSkinnedMeshBoneCount = newSkinnedMesh.bones.Length;
        }
        else
        {
            newSkinnedMeshName = "未指定";
            newSkinnedMeshBoneCount = 0;

        }
        EditorGUILayout.LabelField($"配件骨架名稱：{newSkinnedMeshName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"配件骨頭數量：{newSkinnedMeshBoneCount}", EditorStyles.boldLabel);
        GUI.enabled = AllFieldsAssigned();

        if (GUILayout.Button("Sort (調整骨架順序)"))
        {
            var tool = new GameObject("TempSkinTool").AddComponent<SkinMeshTool>();
            tool.originalSkinnedMesh = originalSkinnedMesh;
            tool.newSkinnedMesh = newSkinnedMesh;
            tool.Sort();
            DestroyImmediate(tool.gameObject);
        }
        if (GUILayout.Button("Replace (原始骨架綁到配件上)"))
        {
            var tool = new GameObject("TempSkinTool").AddComponent<SkinMeshTool>();
            tool.originalSkinnedMesh = originalSkinnedMesh;
            tool.newSkinnedMesh = newSkinnedMesh;
            tool.Replace();
            DestroyImmediate(tool.gameObject);
        }
        GUI.enabled = true;
    }

    private bool AllFieldsAssigned()
    {
        return originalSkinnedMesh != null && newSkinnedMesh != null;
    }
}
