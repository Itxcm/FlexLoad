using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset"; // ab包配置表路径
    public static Dictionary<string, string> allFileDir = new Dictionary<string, string>(); // key: ab包名 Vlaue:文件夹路径

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        HandleFileDir(cf);
        // 处理文件夹

        /*   foreach (var item in cf.filePath)
           {
               Debug.Log(item);
           }*/
        HandleFile(cf);
    }
    // 处理文件夹 将ab包名和文件夹路径对应进行存储
    public static void HandleFileDir(ABConfig cf)
    {
        allFileDir.Clear();
        foreach (var item in cf.fileDirPath)
        {
            if (allFileDir.ContainsKey(item.ABName)) Debug.LogError("AB包配置名重复");
            else allFileDir.Add(item.ABName, item.Path);
        }

    }
    // 处理文件 将指定文件夹下的文件路径进行存储
    public static void HandleFile(ABConfig cf)
    {
        // 获取指定文件夹路径下资源的GUID
        string[] allAssetGUID = AssetDatabase.FindAssets("t:prefab", cf.filePath.ToArray());
        // 根据GUID获取文件夹路径
        for (int i = 0; i < allAssetGUID.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(allAssetGUID[i]);

            // 编辑器进度条
            EditorUtility.DisplayProgressBar("查找prefab", "Prefab:" + path, i * 1.0f / allAssetGUID.Length);
        }
        // 清除精度他
        EditorUtility.ClearProgressBar();
    }
}
