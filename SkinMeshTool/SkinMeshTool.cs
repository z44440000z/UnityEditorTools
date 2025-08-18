using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class SkinMeshTool
{
    public static void Replace(SkinnedMeshRenderer originalSkinnedMesh, SkinnedMeshRenderer newSkinnedMesh)
    {
        if (originalSkinnedMesh == null || newSkinnedMesh == null)
        {
            Debug.LogError("請指定身體和配件的SkinnedMeshRenderer");
            return;
        }
        if (originalSkinnedMesh.bones.Length != newSkinnedMesh.bones.Length)
        {
            Debug.LogError("原始和新SkinnedMeshRenderer的骨頭數量不一致，無法替換骨架。");
            return;
        }
        newSkinnedMesh.rootBone = originalSkinnedMesh.rootBone;
        newSkinnedMesh.bones = originalSkinnedMesh.bones;
        Debug.Log("已替換骨架");
    }

    public static void Sort(SkinnedMeshRenderer originalSkinnedMesh, SkinnedMeshRenderer newSkinnedMesh)
    {
        Transform[] A = originalSkinnedMesh.bones;
        Transform[] B = newSkinnedMesh.bones;

        bool sameBones = AlignAndCheckTransforms(ref A, ref B);

        if (sameBones)
            Debug.Log("兩個骨架內容一致，已對齊順序。");
        else
            Debug.LogWarning("兩個骨架有差異，已嘗試對齊順序。");

        newSkinnedMesh.bones = B; // 把排序後的骨架套回去
    }

    private static bool AlignAndCheckTransforms(ref Transform[] A, ref Transform[] B)
    {
        if (A.Length != B.Length)
        {
            Debug.LogWarning("陣列長度不同");
            return false;
        }

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
                alignedB.Add(null);
            }
        }

        B = alignedB.ToArray();
        return allMatch;
    }

    // 遞歸方法：根據名稱在新的骨架中查找對應的骨頭
    private static Transform FindBoneByName(Transform root, string boneName)
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
