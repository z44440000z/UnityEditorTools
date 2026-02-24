using System;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;

/// <summary>
/// EnvironmentHelper 用於檢測當前運行環境並提供相關資訊
/// 支援的環境包括：Dev、Test、Prod
/// 在 WebGL 平台上，會根據 URL 自動判斷環境並設定 Addressables Profile 中的 HostURL 變數
/// 在 Editor 中預設為 Dev 環境
/// 在 Netlify 上運行時，會自動設定為 Prod 環境
/// </summary>
public static class EnvironmentHelper
{
    static string host;
    public enum EnvironmentType
    {
        Unknown,
        Test,
        Dev,
        Prod
    }

    public static EnvironmentType CurrentEnvironment { get; private set; } = EnvironmentType.Unknown;

    public static void Detect()
    {
        string url = Application.absoluteURL;
#if UNITY_EDITOR
        // 在 Editor 中預設為 Dev 環境
        CurrentEnvironment = EnvironmentType.Dev;
#elif UNITY_WEBGL
        // WebGL：自動從網頁取得目前 URL
        if (!string.IsNullOrEmpty(url))
        {
            Uri uri = new Uri(url);

            string baseUrl = uri.GetLeftPart(UriPartial.Authority); // https://game.memoark.io
            string path = uri.AbsolutePath; // /webar/index.html

            // Path.GetDirectoryName 會在 WebGL 中回傳 "\"，所以要手動處理
            string folder = path;
            int lastSlash = folder.LastIndexOf('/');
            if (lastSlash > 0)
                folder = folder.Substring(0, lastSlash); // -> /webar

            host = baseUrl + folder; // -> https://game.memoark.io/webar

            if (url.Contains("game.memoark.io") || url.Contains("192.168."))
            { CurrentEnvironment = EnvironmentType.Test; }
            else if (url.Contains("memoark.io"))
            { CurrentEnvironment = EnvironmentType.Prod; }
            else if (url.Contains("127.0.0.1") ||url.Contains("localhost") )
            { CurrentEnvironment = EnvironmentType.Dev; }
            else
            { CurrentEnvironment = EnvironmentType.Unknown; }
        }
        else 
        { 
            // 如果無法取得 URL，預設為 Dev 環境
            CurrentEnvironment = EnvironmentType.Dev;
            host = "https://memoark.io/webar/"; 
        }     

        // 設定 Addressables Profile 中的變數值
        AddressablesRuntimeProperties.SetPropertyValue("HostURL", host);
#else
        // 其他平台預設正式 CDN
        host = "https://memoark.io/webar/";
        AddressablesRuntimeProperties.SetPropertyValue("HostURL", host);
        CurrentEnvironment = EnvironmentType.Dev;
#endif
    }

    public static bool IsDev { get { return CurrentEnvironment == EnvironmentType.Dev; } }
    public static bool IsTest { get { return CurrentEnvironment == EnvironmentType.Test; } }
    public static bool IsProd { get { return CurrentEnvironment == EnvironmentType.Prod; } }
}
