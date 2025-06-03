using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class SkinMeshTool : MonoBehaviour
{
    [Tooltip("用作參考的SkinnedMesh(身體)")]
    public SkinnedMeshRenderer originalSkinnedMesh;
    [Tooltip("要替換骨架的SkinnedMesh(配件)")]
    public SkinnedMeshRenderer newSkinnedMesh;

    public void Replace()
    {
        if (originalSkinnedMesh == null || newSkinnedMesh == null)
        {
            Debug.LogError("請指定身體和配件的SkinnedMeshRenderer");
            return;
        }
        // 確保原始SkinnedMeshRenderer的骨頭數量與新SkinnedMeshRenderer的骨頭數量一致
        if (originalSkinnedMesh.bones.Length != newSkinnedMesh.bones.Length)
        {
            Debug.LogError("原始和新SkinnedMeshRenderer的骨頭數量不一致，無法替換骨架。");
            return;
        }
        newSkinnedMesh.rootBone = originalSkinnedMesh.rootBone;
        newSkinnedMesh.bones = originalSkinnedMesh.bones;
        Debug.Log("已替換骨架");
    }

    public void Sort()
    {
        Transform[] A = originalSkinnedMesh.bones;
        Transform[] B = newSkinnedMesh.bones;

        bool sameBones = AlignAndCheckTransforms(ref A, ref B);

        if (sameBones)
        {
            Debug.Log("兩個骨架內容一致，已對齊順序。");
        }
        else
        {
            Debug.LogWarning("兩個骨架有差異，已嘗試對齊順序。");
        }
    }

    bool AlignAndCheckTransforms(ref Transform[] A, ref Transform[] B)
    {
        // 檢查長度是否一致
        if (A.Length != B.Length)
        {
            Debug.LogWarning("陣列長度不同");
            return false;
        }

        // 建立一個 name -> Transform 的字典來快速查找
        Dictionary<string, Transform> bDict = B.ToDictionary(b => b.name, b => b);

        List<Transform> alignedB = new List<Transform>();
        bool allMatch = true;

        foreach (var aBone in A)
        {
            if (bDict.TryGetValue(aBone.name, out Transform matchingBone))
            {
                alignedB.Add(matchingBone);
            }
            else
            {
                Debug.LogWarning($"找不到對應骨頭: {aBone.name}");
                allMatch = false;
                alignedB.Add(null); // 保留位子，避免出錯
            }
        }

        B = alignedB.ToArray(); // 更新 B 順序為 A 一致
        return allMatch;
    }

    // 遞歸方法：根據名稱在新的骨架中查找對應的骨頭
    private Transform FindBoneByName(Transform root, string boneName)
    {
        // 如果當前骨頭名稱匹配，返回該骨頭
        if (root.name == boneName)
        {
            return root;
        }

        // 遞歸查找子骨頭
        foreach (Transform child in root)
        {
            Transform found = FindBoneByName(child, boneName);
            if (found != null)
            {
                return found;
            }
        }

        // 沒有找到，返回 null
        return null;
    }
}
