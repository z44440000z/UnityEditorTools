using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class BatchOperationObjectsEditor : EditorWindow
{
    private enum ObjectCategory { UI, Primitive3D, Prefab }
    private ObjectCategory selectedCategory = ObjectCategory.Primitive3D;
    private PrimitiveType selectedPrimitiveType = PrimitiveType.Cube;
    private GameObject addPrefab;
    private string[] uiTypes = { "Button", "Image", "Text", "Empty UI" };
    private int selectedUIIndex = 0;
    private string baseName = "Object";
    private int startIndex = 0;

    private GameObject rootObject;
    private string searchKeyword = "A";
    private int count = 0;
    private bool equal = false;

    [MenuItem("Tools/批量操作物件")]
    public static void ShowWindow()
    {
        GetWindow<BatchOperationObjectsEditor>("批量操作物件");
    }

    // 更新選中物件數量
    private void Update()
    {
        count = Selection.objects.Length;
        Repaint();  // 強制重新繪製界面，以便及時顯示更新的選中物件數量
    }

    private void OnGUI()
    {
        // 設置區塊樣式
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white, background = Texture2D.grayTexture }
        };

        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("各種批量操作物件的功能", EditorStyles.boldLabel);
        GUILayout.Label($"已選物件數: {count}", EditorStyles.boldLabel);
        GUILayout.Space(50); // 空隙
        GUILayout.EndVertical();

        GUILayout.Label("新增子物件到選中物件", EditorStyles.boldLabel);
        selectedCategory = (ObjectCategory)EditorGUILayout.EnumPopup("類型", selectedCategory);

        if (selectedCategory == ObjectCategory.Primitive3D)
        {
            selectedPrimitiveType = (PrimitiveType)EditorGUILayout.EnumPopup("Primitive 類型", selectedPrimitiveType);
        }
        else if (selectedCategory == ObjectCategory.UI)
        {
            selectedUIIndex = EditorGUILayout.Popup("UI 類型", selectedUIIndex, uiTypes);
        }
        else if (selectedCategory == ObjectCategory.Prefab)
        {
            addPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", addPrefab, typeof(GameObject), true);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("新增子物件"))
        {
            CreateChildObjects();
        }

        GUILayout.Space(10);

        GUILayout.Label("重新命名選中物件", EditorStyles.boldLabel);
        baseName = EditorGUILayout.TextField("名稱", baseName);
        startIndex = EditorGUILayout.IntField("開始數字", startIndex);
        if (GUILayout.Button("開始重新命名"))
        {
            RenameSelectedObjects();
        }

        GUILayout.Space(10);

        GUILayout.Label("搜尋子物件（名稱包含關鍵字）", EditorStyles.boldLabel);
        rootObject = (GameObject)EditorGUILayout.ObjectField("父物件", rootObject, typeof(GameObject), true);
        searchKeyword = EditorGUILayout.TextField("名稱關鍵字", searchKeyword);
        equal = EditorGUILayout.Toggle("完全匹配", equal);

        GUILayout.Space(10);

        if (GUILayout.Button("搜尋並選取"))
        {
            if (rootObject == null || string.IsNullOrEmpty(searchKeyword))
            {
                EditorUtility.DisplayDialog("錯誤", "請指定父物件與搜尋關鍵字", "OK");
                return;
            }

            List<GameObject> matchedObjects = new List<GameObject>();
            SearchChildren(rootObject.transform, searchKeyword, matchedObjects, equal);

            if (matchedObjects.Count > 0)
            {
                Selection.objects = matchedObjects.ToArray();
                Debug.Log($"找到 {matchedObjects.Count} 個符合的子物件！");
            }
            else
            {
                EditorUtility.DisplayDialog("結果", "沒有找到符合的子物件", "OK");
            }
        }
    }

    /// <summary>
    /// 批量創建子物件
    /// </summary>
    private void CreateChildObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("沒有選擇物件", "請先在場景中選取至少一個 GameObject", "OK");
            return;
        }

        foreach (GameObject parent in selectedObjects)
        {
            GameObject newObj = null;

            if (selectedCategory == ObjectCategory.Primitive3D)
            {
                newObj = GameObject.CreatePrimitive(selectedPrimitiveType);
                newObj.name = selectedPrimitiveType.ToString();
            }
            else if (selectedCategory == ObjectCategory.UI)
            {
                newObj = CreateUIElement(uiTypes[selectedUIIndex]);
            }
            else if (selectedCategory == ObjectCategory.Prefab)
            {
                if (addPrefab != null)
                {
                    newObj = PrefabUtility.InstantiatePrefab(addPrefab) as GameObject;
                    newObj.name = addPrefab.name;
                }
                else
                {
                    EditorUtility.DisplayDialog("錯誤", "請選擇一個 Prefab", "OK");
                    return;
                }
            }

            if (newObj != null)
            {
                Undo.RegisterCreatedObjectUndo(newObj, "新增子物件");
                newObj.transform.SetParent(parent.transform, false);
                newObj.transform.localPosition = Vector3.zero;
            }

        }
    }

    private GameObject CreateUIElement(string type)
    {
        GameObject go = null;

        switch (type)
        {
            case "Button":
                go = CreateUIElementWithComponent<Button>("Button");
                break;
            case "Image":
                go = CreateUIElementWithComponent<Image>("Image");
                break;
            case "Text":
                go = CreateUIElementWithComponent<Text>("Text");
                go.GetComponent<Text>().text = "New Text";
                go.GetComponent<Text>().color = Color.black;
                break;
            case "Empty UI":
                go = new GameObject("UIObject", typeof(RectTransform));
                break;
        }

        return go;
    }
    /// <summary>
    /// 創建UI元素並添加組件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    private GameObject CreateUIElementWithComponent<T>(string name) where T : Component
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(T));
        return go;
    }

    // 遞迴獲取完整的 Hierarchy 路徑（父物件/子物件）
    private string GetHierarchyPath(Transform transform)
    {
        if (transform.parent == null)
            return transform.name;
        return GetHierarchyPath(transform.parent) + "/" + transform.GetSiblingIndex().ToString("D4");
    }
    /// <summary>
    /// 批量重新命名選中物件
    /// </summary>
    private void RenameSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("沒有選中物件", "請在 Hierarchy 選擇一或多個物件", "OK");
            return;
        }

        // 按 Hierarchy 上下順序排序（根據在 Hierarchy 中的順序）
        var sortedObjects = selectedObjects.OrderBy(obj =>
        {
            string path = GetHierarchyPath(obj.transform);
            return path;
        }).ToArray();

        Undo.RecordObjects(sortedObjects, "批量重新命名");

        for (int i = 0; i < sortedObjects.Length; i++)
        {
            sortedObjects[i].name = $"{baseName} ({startIndex + i})";
        }

        Debug.Log($"已根據 Hierarchy 順序重新命名 {sortedObjects.Length} 個物件！");
    }


    /// <summary>
    /// 遞迴搜尋子物件，並將符合關鍵字的物件加入結果列表
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="keyword"></param>
    /// <param name="results"></param>
    void SearchChildren(Transform parent, string keyword, List<GameObject> results, bool equal = false)
    {
        if (parent == null) return;

        // 檢查當前物件名稱是否包含關鍵字
        if (equal)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(keyword))
                {
                    results.Add(child.gameObject);
                }
                // 遞迴搜尋
                SearchChildren(child, keyword, results);
            }
        }
        else
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(keyword))
                {
                    results.Add(child.gameObject);
                }
                // 遞迴搜尋
                SearchChildren(child, keyword, results);
            }
        }
    }
}
