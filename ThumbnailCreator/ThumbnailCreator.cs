using System.IO;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.Events;

[Serializable]
public class ThumbnailCreator : ScriptableObject
{
    [SerializeField] public Vector3 CameraPosition = new Vector3(.6f, .3f, .6f);
    [SerializeField] public float CameraMinDistance = 3;
    [SerializeField] public Color BackgroundColor = new Color32(22, 146, 0, 255);

    [SerializeField] public GameObject[] Entities;
    [SerializeField] public string TargetPath = "Icons";


#if UNITY_EDITOR
    /// <summary>
    /// 可自行調整相機位置的生成方式
    /// </summary>
    public void GenerateEntityIcons()
    {
        Debug.Log("正在為物件生成圖示...");

        if (Entities.Length < 1)
        {
            Debug.Log("沒有物件");
            return;
        }

        string path = Application.dataPath + "/" + TargetPath;

        Camera camera = new GameObject("IconCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.aspect = 1;
        camera.backgroundColor = BackgroundColor;
        // camera.orthographic = true;

        foreach (GameObject e in Entities)
        {
            GameObject item = Instantiate(e, Vector3.zero, Quaternion.identity);
            // 設定 RenderTexture
            RenderTexture renderTexture = new RenderTexture(256, 256, 32);
            camera.targetTexture = renderTexture;

            // 調整相機來對準實體
            Bounds bounds = CalculateBounds(item.transform);
            // 計算相機到物體的距離，使其能完整包裹住物體
            float objectSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            // 相機位置固定在 CameraPosition
            Vector3 cameraPosition = bounds.center + CameraPosition;
            camera.transform.position = cameraPosition;

            // 計算相機的最佳 field of view，使物體剛好填滿視野
            float distanceToCenter = (bounds.center - cameraPosition).magnitude;
            float optimalFOV = Mathf.Atan2(objectSize * 0.6f, distanceToCenter) * Mathf.Rad2Deg * 2;
            // 使用最小值約束 FOV，避免太小
            camera.fieldOfView = Mathf.Max(optimalFOV, CameraMinDistance);

            // 相機看向物體的中心點
            camera.transform.LookAt(bounds.center);
            camera.Render();

            // 將 RenderTexture 轉換為 Texture2D
            Texture2D icon = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            icon.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
            icon.Apply();

            // 清理 RenderTexture
            RenderTexture.active = null;
            camera.targetTexture = null;
            DestroyImmediate(renderTexture);

            // 儲存圖片
            byte[] bytes = icon.EncodeToPNG();
            if (!Directory.Exists(path))
            { Directory.CreateDirectory(path); }
            string fullPath = path + "/" + e.name + ".png";
            File.WriteAllBytes(fullPath, bytes);

            // 刪除暫存的圖像
            DestroyImmediate(icon);
            Debug.Log("生成圖示： " + e.name);
            camera.transform.SetParent(null);
            DestroyImmediate(item);
        }

        // 刪除臨時相機
        DestroyImmediate(camera.gameObject);

        Debug.Log("生成完成，儲存圖示到路徑： " + path);
    }

    // 計算實體的邊界來調整相機
    private Bounds CalculateBounds(Transform entity)
    {
        Renderer[] renderers = entity.GetComponentsInChildren<Renderer>();
        MeshFilter[] meshFilters = entity.GetComponentsInChildren<MeshFilter>();
        Bounds bounds = new Bounds(entity.position, Vector3.zero);
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        foreach (MeshFilter filters in meshFilters)
        {
            bounds.Encapsulate(filters.sharedMesh.bounds);
        }
        return bounds;
    }
#endif


}
