using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 這是一個自訂的 Editor Window，用來在 Unity 編輯器中顯示構建時間(BuildTimestamp)與版本號。
/// </summary>
public class BuildInfoWindow : EditorWindow
{
    // BuildTimestamp 資產的實例，用來讀取構建時間
    private BuildTimestamp _buildTimestamp;

    // 版本號字串，預設空字串
    private string _versionNumber = "";

    // 顯示時間的格式
    private string _format = "yyyy/MM/dd HH:mm";
    private bool _isEditFormat;

    // 額外訊息，可由使用者輸入
    private string _Message = "";

    // 在 Unity 編輯器工具列中新增一個選單項目 "Tools/Build Info Window"，點擊會開啟這個視窗
    [MenuItem("Tools/輸出資訊視窗")]
    public static void ShowWindow()
    {
        // 打開或切換到此視窗，標題為 "Build Info"
        GetWindow<BuildInfoWindow>("輸出資訊");
    }


    // 視窗啟用或重新獲得焦點時會被呼叫
    private void OnEnable()
    {
        BuildTimestampRecorder.onBuild += UpdateCustomValue;
        // 載入 BuildTimestamp 資產
        LoadBuildTimestamp();

        // 從 PlayerSettings 讀取當前專案的版本號
        _versionNumber = PlayerSettings.bundleVersion;
        _Message = _buildTimestamp.message;
        // _format = "yyyy/MM/dd HH:mm";
    }

    private void OnDisable()
    {
        BuildTimestampRecorder.onBuild -= UpdateCustomValue;
    }

    // 繪製視窗的 UI
    private void OnGUI()
    {
        string timestamp = _buildTimestamp ? _buildTimestamp.ToString(_format) : "";
        string newFormat = _format;

        // 大標題，顯示在視窗最上方
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("輸出工具", EditorStyles.boldLabel);
        // 模擬右上角位置的三個點按鈕
        float height = EditorGUIUtility.singleLineHeight;
        float width = height; // 正方形按鈕
        Rect buttonRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width));

        if (GUI.Button(buttonRect, "...", EditorStyles.label))
        {
            ShowDotsMenu(buttonRect);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.ObjectField("BuildTimestamp: ", _buildTimestamp, typeof(BuildTimestamp), false);
        // 按鈕：點擊時重新讀取 BuildTimestamp 資產
        if (GUILayout.Button("讀取 Asset", GUILayout.Width(100)))
        {
            LoadBuildTimestamp();
            UpdateCustomValue();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(); // 空白間距

        // 建立一個帶框線的區塊
        EditorGUILayout.BeginVertical("box");

        // 顯示版本號，使用 TextField (可編輯文字框，但這裡不會寫回)
        EditorGUI.BeginChangeCheck();
        string newVersionNumber = EditorGUILayout.TextField("版本號:", _versionNumber);


        // 顯示標籤「Build 時間:」
        if (_isEditFormat)
        {
            EditorGUILayout.BeginHorizontal();
            newFormat = EditorGUILayout.TextField("日期格式:", _format);
            if (GUILayout.Button("確定", GUILayout.Width(100)))
            { EditFormat(); }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build 時間: ", GUILayout.Width(EditorGUIUtility.labelWidth));

        // 如果找到 BuildTimestamp 資產，就用指定格式顯示時間字串
        if (_buildTimestamp != null)
        {
            EditorGUILayout.LabelField(timestamp);
        }
        else
        {
            // 找不到資產時，顯示警告訊息
            EditorGUILayout.HelpBox("BuildTimestamp asset not found.", MessageType.Warning);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // 額外訊息的標籤
        EditorGUILayout.LabelField("Message:");

        // 可編輯的多行文字區塊，使用者可以輸入額外訊息
        _Message = EditorGUILayout.TextArea(_Message, GUILayout.Height(height * 6));

        // 結束帶框線的區塊
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        if (EditorGUI.EndChangeCheck())
        {
            _format = newFormat;
            _versionNumber = newVersionNumber;
            if (PlayerSettings.bundleVersion != _versionNumber)
            {
                PlayerSettings.bundleVersion = _versionNumber;
                Debug.Log($"版本號變更為: {_versionNumber}");
            }
            _buildTimestamp.SetMessage(_Message);
        }
        EditorGUILayout.BeginHorizontal();
        // 按鈕：點擊時把版本號複製到系統剪貼簿，方便貼上使用
        if (GUILayout.Button("複製版本號"))
        {
            EditorGUIUtility.systemCopyBuffer = _versionNumber;
            Debug.Log("已複製版本號");
        }
        GUILayout.Space(height);
        // 按鈕：點擊時把更新訊息複製到系統剪貼簿，方便貼上使用
        if (GUILayout.Button("複製貼到Git上的訊息"))
        {
            EditorGUIUtility.systemCopyBuffer = $"[V{_versionNumber}]\n" + _Message;
            Debug.Log("已複製訊息");
        }
        EditorGUILayout.EndHorizontal();
    }

    public void UpdateCustomValue()
    {
        string timestamp = _buildTimestamp ? _buildTimestamp.ToString(_format) : "";
        PlayerSettings.SetTemplateCustomValue("BUILD_TIME", timestamp);
    }

    /// <summary>
    /// 從專案中搜尋並載入第一個找到的 BuildTimestamp 資產。
    /// </summary>
    private void LoadBuildTimestamp()
    {
        // 用資產類型過濾查詢 BuildTimestamp 類型的資產，回傳第一筆 GUID
        string guid = AssetDatabase.FindAssets("t:BuildTimestamp").FirstOrDefault();

        if (string.IsNullOrEmpty(guid))
        {
            // 找不到就清空變數
            _buildTimestamp = null;
            return;
        }

        // 從 GUID 轉成資產路徑
        string path = AssetDatabase.GUIDToAssetPath(guid);

        // 從路徑載入資產實例
        _buildTimestamp = AssetDatabase.LoadAssetAtPath<BuildTimestamp>(path);
    }
    [ContextMenu("編輯日期格式")]
    private void EditFormat()
    {
        _isEditFormat = !_isEditFormat;
    }

    private void ShowDotsMenu(Rect buttonRect)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("編輯日期格式"), false, () => EditFormat());
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("關閉視窗"), false, Close);

        // 注意：用 ShowAsContext() 會出現在滑鼠位置
        // 為了對齊我們的 button，用 DropDown 傳入 buttonRect
        menu.DropDown(buttonRect);
    }


}
