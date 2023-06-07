using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset"; // ab包配置表路径
    public static List<string> allFileList = new List<string>(); // 所有AB包文件夹路径 需要过滤的列表
    public static Dictionary<string, List<string>> prefabDic = new Dictionary<string, List<string>>(); // 单个prefab字典 ab包名 : 依赖路径列表
    public static Dictionary<string, string> fileDirDic = new Dictionary<string, string>(); // 所有文件夹ab包字典 ab包名 : 路径 

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        // 处理需要打包的
        HandleFileDir(cf);
        HandlePrfab(cf);

        // 设置AB标签
        SetABLabelByAllDic();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //  ClearABLabel();



        // 清楚进度条
        EditorUtility.ClearProgressBar();
    }
    // 处理文件夹 将文件夹路径 和 指定名字 作为字典存储
    public static void HandleFileDir(ABConfig cf)
    {
        prefabDic.Clear();
        fileDirDic.Clear();
        allFileList.Clear();
        foreach (var item in cf.fileDirPathList)
        {
            if (fileDirDic.ContainsKey(item.ABName)) Debug.LogError("AB配置表文件夹AB包配置名重复!");
            else
            {
                fileDirDic.Add(item.ABName, item.Path);
                allFileList.Add(item.Path);
            }
        }
    }
    // 处理文件 将文件夹下每一个prefabs 以 名称 和 依赖路径列表 作为字典存储
    public static void HandlePrfab(ABConfig cf)
    {
        // 获取指定文件夹路径下资源的GUID
        string[] allAssetGUID = AssetDatabase.FindAssets("t:prefab", cf.prefabPathList.ToArray());
        // 根据GUID获取文件路径
        for (int i = 0; i < allAssetGUID.Length; i++)
        {
            // 获取单个文件路径
            var path = AssetDatabase.GUIDToAssetPath(allAssetGUID[i]);
            // 编辑器进度条
            EditorUtility.DisplayProgressBar("查找prefab", "Prefab:" + path, i * 1.0f / allAssetGUID.Length);

            if (AllFileDirContainPath(path)) Debug.LogError("AB配置表单个文件路径与文件夹路径重复!");
            else
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                // 获取指定路径文件的所有依赖
                string[] alldps = AssetDatabase.GetDependencies(path);
                List<string> dpPathList = new List<string>();
                foreach (var dpPath in alldps)
                {
                    if (dpPath.EndsWith(".cs")) continue; // 跳过依赖的cs文件
                    if (AllFileDirContainPath(dpPath)) Debug.LogErrorFormat("Prefab中依赖文件路径与文件夹路径重复! : {0}", dpPath);
                    else
                    {
                        dpPathList.Add(dpPath);
                        allFileList.Add(dpPath);
                    }
                }
                if (prefabDic.ContainsKey(go.name)) Debug.LogErrorFormat("存在相同名字的prefab! : {0}", go.name);
                else prefabDic.Add(go.name, dpPathList);
            }
        }
    }
    // 根据配置好的字典 将文件或文件夹 设置AB标签
    public static void SetABLabelByAllDic()
    {
        foreach (var item in prefabDic) SetABlabel(item.Key, item.Value);
        foreach (var item in fileDirDic) SetABlabel(item.Key, item.Value);
    }
    // 清楚AB包标签 此处执行后编辑器标签为空白
    public static void ClearABLabel()
    {
        string[] allBdNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < allBdNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(allBdNames[i], true);
            EditorUtility.DisplayProgressBar("清楚AB标签", "名称:" + allBdNames[i], i * 1.0f / allBdNames.Length);
        }
    }

    #region AB包标签
    public static void SetABlabel(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null) Debug.LogErrorFormat("不存在此路径! : {0}", path);
        else assetImporter.assetBundleName = name;
    }
    public static void SetABlabel(string name, List<string> pathList)
    {
        foreach (string path in pathList) SetABlabel(name, path);
    }
    #endregion AB包标签

    #region 通用判断方法
    // 判断指定路径是否已经包含在文件夹路径中
    public static bool AllFileDirContainPath(string path)
    {
        for (int i = 0; i < allFileList.Count; i++)
        {
            if (path == allFileList[i] || allFileList.Contains(path)) return true;
        }
        return false;
    }
    #endregion
}
