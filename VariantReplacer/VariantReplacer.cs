using UnityEditor;
using UnityEngine;

public class VariantReplacer : EditorWindow
{
    GameObject prefabRoot;
    GameObject variantA;
    GameObject variantB;

    [MenuItem("Tools/替換工具/Variant 替換")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<VariantReplacer>("Variant 替換工具");
    }

    void OnGUI()
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
        EditorGUILayout.BeginVertical(boxStyle);
        GUILayout.Label("動物Prefab需要替換模型的時候，可以用此工具一鍵替換。\n此工具會一併更改相同骨架的<b>Tag</b>和<b>Layer</b>，並將原本夾雜在舊模型中的<b>子物件</b>挪到新模型。", helpStyle);
        GUILayout.Space(50); // 空隙
        GUILayout.EndVertical();

        prefabRoot = (GameObject)EditorGUILayout.ObjectField("Prefab 物件", prefabRoot, typeof(GameObject), true);
        variantA = (GameObject)EditorGUILayout.ObjectField("Variant A (要被替換)", variantA, typeof(GameObject), true);
        variantB = (GameObject)EditorGUILayout.ObjectField("Variant B (替換用)", variantB, typeof(GameObject), true);
        GUILayout.Space(10); // 空隙
        if (GUILayout.Button("執行替換"))
        {
            if (prefabRoot && variantA && variantB)
            {
                ReplaceVariant();
            }
            else
            {
                Debug.LogError("請設定所有欄位。");
            }
        }
    }

    void ReplaceVariant()
    {
        Transform found = FindChildByName(prefabRoot.transform, variantA.name);
        if (found == null)
        {
            Debug.LogError("找不到 Variant A 於 Prefab 中。");
            return;
        }

        GameObject newVariant = (GameObject)PrefabUtility.InstantiatePrefab(variantB);
        newVariant.name = variantA.name;

        newVariant.transform.SetParent(found.parent);
        newVariant.transform.localPosition = found.localPosition;
        newVariant.transform.localRotation = found.localRotation;
        newVariant.transform.localScale = found.localScale;

        // 同步屬性
        MergeDataRecursive(found.gameObject, newVariant);

        // 刪除原始
        DestroyImmediate(found.gameObject);

        Debug.Log("替換完成。");
    }

    void MergeDataRecursive(GameObject source, GameObject target)
    {
        // 複製 Tag 和 Layer
        target.tag = source.tag;
        target.layer = source.layer;

        // 移除並複製 Collider
        foreach (var collider in target.GetComponents<Collider>())
            DestroyImmediate(collider);

        foreach (var sourceCol in source.GetComponents<Collider>())
        {
            var col = target.AddComponent(sourceCol.GetType()) as Collider;
            EditorUtility.CopySerialized(sourceCol, col);
        }

        // 根據 index 合併子物件
        int sourceChildCount = source.transform.childCount;
        int targetChildCount = target.transform.childCount;

        for (int i = 0; i < sourceChildCount; i++)
        {
            Transform sourceChild = source.transform.GetChild(i);

            if (i < targetChildCount)
            {
                Transform targetChild = target.transform.GetChild(i);
                MergeDataRecursive(sourceChild.gameObject, targetChild.gameObject);
            }
            else
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(sourceChild.gameObject) &&
                    PrefabUtility.GetCorrespondingObjectFromSource(sourceChild.gameObject) != null &&
                    PrefabUtility.GetPrefabAssetType(sourceChild.gameObject) != PrefabAssetType.NotAPrefab)
                {
                    GameObject prefab = (GameObject)PrefabUtility.GetCorrespondingObjectFromOriginalSource(sourceChild.gameObject);
                    // 這裡的 InstantiatePrefab 會自動處理 Prefab 的實例化，並且不會帶上原本的父物件
                    GameObject copied = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target.transform);
                    // copied.transform.SetParent(target.transform, false); // 正確設置為目標的子物件
                    copied.name = sourceChild.name;
                }
                else
                {
                    GameObject copied = Instantiate(sourceChild.gameObject, target.transform);
                    copied.transform.SetParent(target.transform, false); // 確保不會自動帶上原本父物件
                    copied.name = sourceChild.name;
                }
            }
        }
    }



    Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            var found = FindChildByName(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
