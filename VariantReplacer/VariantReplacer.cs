using UnityEditor;
using UnityEngine;

public class VariantReplacer : EditorWindow
{
    GameObject prefabRoot;
    GameObject variantA;
    GameObject variantB;

    [MenuItem("Tools/替換工具/Variant 替換工具")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<VariantReplacer>("Variant 替換工具");
    }

    void OnGUI()
    {
        prefabRoot = (GameObject)EditorGUILayout.ObjectField("Prefab Root", prefabRoot, typeof(GameObject), true);
        variantA = (GameObject)EditorGUILayout.ObjectField("Variant A (要被替換)", variantA, typeof(GameObject), true);
        variantB = (GameObject)EditorGUILayout.ObjectField("Variant B (替換用)", variantB, typeof(GameObject), true);

        if (GUILayout.Button("執行替換"))
        {
            if (prefabRoot && variantA && variantB)
            {
                ReplaceVariant();
            }
            else
            {
                Debug.LogWarning("請設定所有欄位。");
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
                    GameObject copied = (GameObject)PrefabUtility.InstantiatePrefab(prefab,target.transform);
                    // copied.transform.SetParent(target.transform, false); // 正確設置為目標的子物件
                    copied.name = sourceChild.name;
                }
                else
                {
                    GameObject copied = Instantiate(sourceChild.gameObject,target.transform);
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
