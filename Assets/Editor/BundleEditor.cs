using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class BundleEditor
{
    public static string BYTESOUTPUTPAHT = Application.dataPath + "/Config/AssetBundleConfig.bytes"; // 配置表二进制bytes输出路径
    public static string XMLOUTPUTPATH = Application.dataPath + "/Config/AssetBundleConfig.Xml"; // 配置表Xml输出路径
    public static string BUILDTARGETPATH = Application.streamingAssetsPath; // 打包输出路径
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset"; // ab包配置表路径
    public static List<string> allABFileList = new List<string>(); // 所有AB包文件夹路径 需要过滤的列表
    public static Dictionary<string, List<string>> prefabDic = new Dictionary<string, List<string>>(); // 单个prefab字典 ab包名 : 依赖路径列表
    public static Dictionary<string, string> fileDirDic = new Dictionary<string, string>(); // 所有文件夹ab包字典 ab包名 : 路径 
    public static List<string> validatePathList = new List<string>(); // 有效路径列表 (打包文件夹路径和所有prefab路径)

    [MenuItem("Tools/打包")]
    public static void Build()
    {
        // 初始化
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        prefabDic.Clear();
        fileDirDic.Clear();
        allABFileList.Clear();
        validatePathList.Clear();

        // 文件处理成响应的字典
        HandleFileDir(cf);
        HandlePrfab(cf);

        // 设置AB标签
        SetABLabelByAllDic();
        // 打包
        BuildAssetBundle();
        // 清除AB标签
        ClearABLabel();

        // 刷新 关闭进度条
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    #region 配置表

    /// <summary>
    /// 写入打包配置配置
    /// </summary>
    /// <param name="pathABNameDic">路径 : AB包名</param>
    private static void WriteData(Dictionary<string, string> pathABNameDic)
    {
        AssetBundleConfig cf = new AssetBundleConfig();
        cf.ABList = new List<ABBase>();

        foreach (var item in pathABNameDic)
        {
            string abNameV = item.Value;
            string abPathK = item.Key;
            if (!IsValidatePath(abPathK)) continue; // 不是有效路径
            ABBase ab = new ABBase();
            ab.ABName = abNameV; // ab包名
            ab.Path = abPathK; // ab包路径
            ab.Crc = Crc32.GetCrc32(abPathK); // 路径对应的唯一crc
            ab.AssetName = abPathK.Remove(0, abPathK.LastIndexOf("/") + 1); // ab资源名称
            ab.ABDependence = new List<string>(); // ab依赖项

            // 根据ab包路径 获取所有依赖路径 
            string[] dbPaths = AssetDatabase.GetDependencies(abPathK);
            for (int i = 0; i < dbPaths.Length; i++)
            {
                // 排除自身和脚本文件
                if (abPathK == dbPaths[i] || abPathK.EndsWith("cs")) continue;

                // 从所有ab包字典 找查找当前路径的ab包名 不一样的进行记录
                if (pathABNameDic.TryGetValue(dbPaths[i], out string abName))
                {
                    if (abName == abNameV) continue; // 排除自身
                    if (!ab.ABDependence.Contains(abName)) ab.ABDependence.Add(abName); // 添加依赖的其他bundle名称
                }
            }
            cf.ABList.Add(ab);
        }

        // 写入生成Xml文件
        CreateXmlFile(cf);

        // 写入生成二进制文件
        CreateBytesFile(cf);
    }
    /// <summary>
    ///  根据配置生成Xml文件
    /// </summary>
    /// <param name="cf"></param>
    private static void CreateXmlFile(AssetBundleConfig cf)
    {
        using FileStream fs = new FileStream(XMLOUTPUTPATH, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using StreamWriter sw = new StreamWriter(fs);
        XmlSerializer xs = new XmlSerializer(typeof(AssetBundleConfig));
        xs.Serialize(sw, cf);
    }
    /// <summary>
    /// 根据配置生成二进制文件
    /// </summary>
    /// <param name="cf"></param>
    private static void CreateBytesFile(AssetBundleConfig cf)
    {
        // 清除路径字符串 用Crc代替了路径
        for (int i = 0; i < cf.ABList.Count; i++) cf.ABList[i].Path = "";
        using FileStream fs = new FileStream(BYTESOUTPUTPAHT, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, cf);
    }

    #endregion 配置表

    #region 文件处理成对应的字典

    /// <summary>
    /// 处理文件夹 将文件夹路径 和 指定名字 作为字典存储
    /// </summary>
    /// <param name="cf"></param>
    private static void HandleFileDir(ABConfig cf)
    {
        foreach (var item in cf.fileDirPathList)
        {
            if (fileDirDic.ContainsKey(item.ABName)) Debug.LogError("AB配置表文件夹AB包配置名重复!");
            else
            {
                fileDirDic.Add(item.ABName, item.Path);
                allABFileList.Add(item.Path);
                validatePathList.Add(item.Path);
            }
        }
    }
    /// <summary>
    ///  // 处理文件 将文件夹下每一个prefabs 以 名称 和 依赖路径列表 作为字典存储
    /// </summary>
    /// <param name="cf"></param>
    private static void HandlePrfab(ABConfig cf)
    {
        // 获取指定文件夹路径下资源的GUID
        string[] allAssetGUID = AssetDatabase.FindAssets("t:prefab", cf.prefabPathList.ToArray());
        // 根据GUID获取文件路径
        for (int i = 0; i < allAssetGUID.Length; i++)
        {
            // 获取单个文件路径
            var path = AssetDatabase.GUIDToAssetPath(allAssetGUID[i]);
            validatePathList.Add(path);
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
                    if (dpPath.EndsWith(".cs") || AllFileDirContainPath(dpPath)) continue; // 跳过依赖的cs文件  || prefab中的依赖在 其他打包文件夹中存在(通过路径名包含了)
                    else
                    {
                        dpPathList.Add(dpPath);
                        allABFileList.Add(dpPath);
                    }
                }
                if (prefabDic.ContainsKey(go.name)) Debug.LogErrorFormat("存在相同名字的prefab! : {0}", go.name);
                else prefabDic.Add(go.name, dpPathList);
            }
        }
    }

    #endregion 文件处理成对应的字典

    #region AB包处理

    /// <summary>
    ///  打包AssetBundle
    /// </summary>
    private static void BuildAssetBundle()
    {
        // 获取所有AB标记
        string[] allBundNames = AssetDatabase.GetAllAssetBundleNames();
        // 路径 Ab包名 字典
        Dictionary<string, string> pathABNameDic = new Dictionary<string, string>();

        for (int i = 0; i < allBundNames.Length; i++)
        {
            // 获取该标记的所有资源路径
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(allBundNames[i]);
            for (int j = 0; j < assetPaths.Length; j++)
            {
                if (assetPaths[j].EndsWith("cs") || !IsValidatePath(assetPaths[j])) continue; // 去除非有效路径和cs文件
                pathABNameDic.Add(assetPaths[j], allBundNames[i]); // 将该资源和路径进行存储
            }
        }

        // 删除改变的AB包
        DelAssetBundle();

        // 写入配置表
        WriteData(pathABNameDic);

        // API打包
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(BUILDTARGETPATH, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null) Debug.LogError("AssetBundle 打包失败！");
        else Debug.Log("AssetBundle 打包完毕");
    }
    /// <summary>
    ///  删除改变的AB包
    /// </summary>
    private static void DelAssetBundle()
    {
        string[] abLabels = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo dc = new DirectoryInfo(BUILDTARGETPATH);
        FileInfo[] files = dc.GetFiles("*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Name.EndsWith(".meta") || files[i].Name.EndsWith(".manifest") || ABLabelsContainName(files[i].Name, abLabels)) continue; // 此包包含设置的Ab标签或者是meta文件
            else
            {
                // 存在则删除之前的无用包
                if (File.Exists(files[i].FullName)) File.Delete(files[i].FullName);
                if (File.Exists(files[i].FullName + ".manifest")) File.Delete(files[i].FullName + ".manifest");
                if (File.Exists(files[i].FullName + ".meta")) File.Delete(files[i].FullName + ".meta");
                if (File.Exists(files[i].FullName + ".manifest.meta")) File.Delete(files[i].FullName + ".manifest.meta");
            }
        }
    }
    /// <summary>
    /// 判断该AB包名称 是否在存在 当前的AB标签中
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private static bool ABLabelsContainName(string name, string[] abLabels)
    {
        for (int i = 0; i < abLabels.Length; i++)
        {
            if (abLabels[i] == name) return true;
        }
        return false;
    }

    #endregion AB包处理

    #region AB包标签

    private static void SetABlabel(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null) Debug.LogErrorFormat("不存在此路径! : {0}", path);
        else assetImporter.assetBundleName = name;
    }
    private static void SetABlabel(string name, List<string> pathList)
    {
        foreach (string path in pathList) SetABlabel(name, path);
    }
    /// <summary>
    /// 根据配置好的字典 将文件或文件夹 设置AB标签
    /// </summary>
    private static void SetABLabelByAllDic()
    {
        foreach (var item in fileDirDic) SetABlabel(item.Key, item.Value);
        foreach (var item in prefabDic) SetABlabel(item.Key, item.Value);
    }
    /// <summary>
    /// 清除AB包标签 此处执行后编辑器标签为空白
    /// </summary>
    private static void ClearABLabel()
    {
        string[] allBdNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < allBdNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(allBdNames[i], true);
            EditorUtility.DisplayProgressBar("清楚AB标签", "名称:" + allBdNames[i], i * 1.0f / allBdNames.Length);
        }
    }

    #endregion AB包标签

    #region 通用判断方法

    /// <summary>
    /// 判断指定路径是否已经包含在文件夹路径中 用于冗余剔除
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool AllFileDirContainPath(string path)
    {
        for (int i = 0; i < allABFileList.Count; i++)
        {
            if ((path == allABFileList[i] || path.Contains(allABFileList[i])) && (path.Replace(allABFileList[i], "")[0] == '/')) return true;
        }
        return false;
    }
    /// <summary>
    /// 是否是有效路径(文件夹路径或者Prefab路径)
    /// </summary>
    /// <returns></returns>
    private static bool IsValidatePath(string path)
    {
        for (int i = 0; i < validatePathList.Count; i++)
        {
            if (path.Contains(validatePathList[i])) return true;
        }
        return false;
    }
    #endregion 通用判断方法
}
