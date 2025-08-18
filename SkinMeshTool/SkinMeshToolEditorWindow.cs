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

    [MenuItem("Tools/替換工具/Skinned Mesh替換")]
    public static void ShowWindow()
    {
        GetWindow<SkinMeshToolEditorWindow>("Skinned Mesh替換工具");
    }

    private void OnGUI()
    {
        // 設置區塊樣式
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.normal.textColor = Color.white;
        boxStyle.normal.background = Texture2D.grayTexture;

        GUIStyle helpStyle = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            fontSize = 13,
            richText = true
        };
        // 開始垂直佈局
        EditorGUILayout.BeginVertical(boxStyle);
        // 在區塊內顯示文字
        GUILayout.Label("💀 骨架替換工具，主要是為了快速替換 Skinned Mesh Renderer 的骨架（通常用於換裝系統或模型配件匹配）。\n\nSort：對[配件]的 bones 陣列排序，對齊[身體]的骨骼順序。\n\nReplace：將[配件]的骨骼參考替換成[身體]的 bones，使配件跟角色共用同一套骨骼", helpStyle);
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
            SkinMeshTool.Sort(originalSkinnedMesh, newSkinnedMesh);
        }

        if (GUILayout.Button("Replace (原始骨架綁到配件上)"))
        {
            SkinMeshTool.Replace(originalSkinnedMesh, newSkinnedMesh);
        }
        GUI.enabled = true;
    }

    private bool AllFieldsAssigned()
    {
        return originalSkinnedMesh != null && newSkinnedMesh != null;
    }
}
