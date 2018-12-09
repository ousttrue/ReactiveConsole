using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace ReactiveConsole
{
    public static class UnityPathUtil
    {
        public static string GetFullPath(UnityEngine.Object asset)
        {
            var unityPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(unityPath))
            {
                throw new ArgumentException();
            }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(dir, unityPath).Replace("\\", "/");
        }
    }
}
