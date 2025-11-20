using UnityEditor;
using UnityEngine;

public class ToolWindow : EditorWindow
{
    private int selectedTabIndex = 0; // 用於記錄當前選中的 Tab
    private string[] tabNames = new string[] { "Tool 1", "Tool 2", "Tool 3" }; // Tab 名稱

    // 開啟編輯器窗口的選單項
    [MenuItem("Tools/ToolWindow")]
    public static void ShowWindow()
    {
        // 顯示窗口
        ToolWindow window = GetWindow<ToolWindow>("Tools");
        window.Show();
    }

    private void OnGUI()
    {
        // 顯示 Tab 切換 UI，並設置選中 Tab
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabNames);

        // 根據選中的 Tab 顯示不同的工具界面
        switch (selectedTabIndex)
        {
            case 0:
                DrawTool1();
                break;
            case 1:
                DrawTool2();
                break;
            case 2:
                DrawTool3();
                break;
        }
    }

    // 顯示工具 1 的界面
    private void DrawTool1()
    {
        GUILayout.Label("這是工具 1", EditorStyles.boldLabel);
        if (GUILayout.Button("工具 1 按鈕"))
        {
            Debug.Log("工具 1 按鈕被點擊");
        }
    }

    // 顯示工具 2 的界面
    private void DrawTool2()
    {
        GUILayout.Label("這是工具 2", EditorStyles.boldLabel);
        if (GUILayout.Button("工具 2 按鈕"))
        {
            Debug.Log("工具 2 按鈕被點擊");
        }
    }

    // 顯示工具 3 的界面
    private void DrawTool3()
    {
        GUILayout.Label("這是工具 3", EditorStyles.boldLabel);
        if (GUILayout.Button("工具 3 按鈕"))
        {
            Debug.Log("工具 3 按鈕被點擊");
        }
    }
}
