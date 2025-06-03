using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ThumbnailCreatorEditorWindow : EditorWindow
{
    private SerializedObject serializedObject; // 用於序列化對象
    private Vector3 cameraPosition;
    private float cameraMinDistance;
    private Color backgroundColor;
    private SerializedProperty entitiesProperty;
    private string targetPath;
    private ThumbnailCreator target;  // 用來保存生成的ScriptableObject

    [MenuItem("Tools/縮圖生成器")]
    public static void ShowWindow()
    {
        var window = GetWindow<ThumbnailCreatorEditorWindow>(false, "縮圖生成器");
        window.Show(); //展示視窗
    }
    private void OnEnable()
    {
        // 在這裡加載或創建一個 ScriptableObject 實例
        // target = AssetDatabase.LoadAssetAtPath<ThumbnailCreator>("Assets/ThumbnailCreator.asset");
        // if (target == null)
        // {
        //     AssetDatabase.CreateAsset(target, "Assets/ThumbnailCreator.asset");
        //     AssetDatabase.SaveAssets();
        // }
        Init();
    }

    void Init()
    {
        if (target == null)
        {
            target = ScriptableObject.CreateInstance<ThumbnailCreator>();
        }

        if (serializedObject == null || serializedObject.targetObject != target)
        {
            serializedObject = new SerializedObject(target);
            entitiesProperty = serializedObject.FindProperty("Entities");
        }
    }


    private void OnGUI()
    {
        Init();
        // 更新 SerializedObject
        serializedObject.Update();

        // 設置區塊樣式
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.normal.textColor = Color.white;
        boxStyle.normal.background = Texture2D.grayTexture;

        // 開始垂直佈局
        EditorGUILayout.BeginVertical(boxStyle);
        // 在區塊內顯示文字
        GUILayout.Label("用來生成物件縮圖的小工具", EditorStyles.whiteLabel);
        GUILayout.Space(50); // 空隙
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        EditorGUI.BeginChangeCheck();

        target.TargetPath = EditorGUILayout.TextField("路徑資料夾", target.TargetPath);
        target.CameraPosition = EditorGUILayout.Vector3Field("相機位置", target.CameraPosition);
        target.CameraMinDistance = EditorGUILayout.FloatField("Min Distance", target.CameraMinDistance);
        target.BackgroundColor = EditorGUILayout.ColorField("背景顏色", target.BackgroundColor);

        EditorGUILayout.PropertyField(entitiesProperty, new GUIContent("物件列表"), true);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target); // 標記已修改，若為 ScriptableObject 時有用
        }

        GUILayout.Space(10);
        if (GUILayout.Button("生成縮圖"))
        {
            target.CameraPosition = cameraPosition;
            target.CameraMinDistance = cameraMinDistance;
            target.BackgroundColor = backgroundColor;
            target.GenerateEntityIcons();
        }
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // 應用修改
        serializedObject.ApplyModifiedProperties();
    }
}
