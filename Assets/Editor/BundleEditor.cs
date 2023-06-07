using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class BundleEditor
{
    public static string BYTESOUTPUTPAHT = Application.dataPath + "/Config/AssetBundleConfig.bytes"; // ���ñ������bytes���·��
    public static string XMLOUTPUTPATH = Application.dataPath + "/Config/AssetBundleConfig.Xml"; // ���ñ�Xml���·��
    public static string BUILDTARGETPATH = Application.streamingAssetsPath; // ������·��
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset"; // ab�����ñ�·��
    public static List<string> allABFileList = new List<string>(); // ����AB���ļ���·�� ��Ҫ���˵��б�
    public static Dictionary<string, List<string>> prefabDic = new Dictionary<string, List<string>>(); // ����prefab�ֵ� ab���� : ����·���б�
    public static Dictionary<string, string> fileDirDic = new Dictionary<string, string>(); // �����ļ���ab���ֵ� ab���� : ·�� 
    public static List<string> validatePathList = new List<string>(); // ��Ч·���б� (����ļ���·��������prefab·��)

    [MenuItem("Tools/���")]
    public static void Build()
    {
        // ��ʼ��
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);
        prefabDic.Clear();
        fileDirDic.Clear();
        allABFileList.Clear();
        validatePathList.Clear();

        // �ļ��������Ӧ���ֵ�
        HandleFileDir(cf);
        HandlePrfab(cf);

        // ����AB��ǩ
        SetABLabelByAllDic();
        // ���
        BuildAssetBundle();
        // ���AB��ǩ
        ClearABLabel();

        // ˢ�� �رս�����
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
    }

    #region ���ñ�

    /// <summary>
    /// д������������
    /// </summary>
    /// <param name="pathABNameDic">·�� : AB����</param>
    private static void WriteData(Dictionary<string, string> pathABNameDic)
    {
        AssetBundleConfig cf = new AssetBundleConfig();
        cf.ABList = new List<ABBase>();

        foreach (var item in pathABNameDic)
        {
            string abNameV = item.Value;
            string abPathK = item.Key;
            if (!IsValidatePath(abPathK)) continue; // ������Ч·��
            ABBase ab = new ABBase();
            ab.ABName = abNameV; // ab����
            ab.Path = abPathK; // ab��·��
            ab.Crc = Crc32.GetCrc32(abPathK); // ·����Ӧ��Ψһcrc
            ab.AssetName = abPathK.Remove(0, abPathK.LastIndexOf("/") + 1); // ab��Դ����
            ab.ABDependence = new List<string>(); // ab������

            // ����ab��·�� ��ȡ��������·�� 
            string[] dbPaths = AssetDatabase.GetDependencies(abPathK);
            for (int i = 0; i < dbPaths.Length; i++)
            {
                // �ų�����ͽű��ļ�
                if (abPathK == dbPaths[i] || abPathK.EndsWith("cs")) continue;

                // ������ab���ֵ� �Ҳ��ҵ�ǰ·����ab���� ��һ���Ľ��м�¼
                if (pathABNameDic.TryGetValue(dbPaths[i], out string abName))
                {
                    if (abName == abNameV) continue; // �ų�����
                    if (!ab.ABDependence.Contains(abName)) ab.ABDependence.Add(abName); // �������������bundle����
                }
            }
            cf.ABList.Add(ab);
        }

        // д������Xml�ļ�
        CreateXmlFile(cf);

        // д�����ɶ������ļ�
        CreateBytesFile(cf);
    }
    /// <summary>
    ///  ������������Xml�ļ�
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
    /// �����������ɶ������ļ�
    /// </summary>
    /// <param name="cf"></param>
    private static void CreateBytesFile(AssetBundleConfig cf)
    {
        // ���·���ַ��� ��Crc������·��
        for (int i = 0; i < cf.ABList.Count; i++) cf.ABList[i].Path = "";
        using FileStream fs = new FileStream(BYTESOUTPUTPAHT, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, cf);
    }

    #endregion ���ñ�

    #region �ļ�����ɶ�Ӧ���ֵ�

    /// <summary>
    /// �����ļ��� ���ļ���·�� �� ָ������ ��Ϊ�ֵ�洢
    /// </summary>
    /// <param name="cf"></param>
    private static void HandleFileDir(ABConfig cf)
    {
        foreach (var item in cf.fileDirPathList)
        {
            if (fileDirDic.ContainsKey(item.ABName)) Debug.LogError("AB���ñ��ļ���AB���������ظ�!");
            else
            {
                fileDirDic.Add(item.ABName, item.Path);
                allABFileList.Add(item.Path);
                validatePathList.Add(item.Path);
            }
        }
    }
    /// <summary>
    ///  // �����ļ� ���ļ�����ÿһ��prefabs �� ���� �� ����·���б� ��Ϊ�ֵ�洢
    /// </summary>
    /// <param name="cf"></param>
    private static void HandlePrfab(ABConfig cf)
    {
        // ��ȡָ���ļ���·������Դ��GUID
        string[] allAssetGUID = AssetDatabase.FindAssets("t:prefab", cf.prefabPathList.ToArray());
        // ����GUID��ȡ�ļ�·��
        for (int i = 0; i < allAssetGUID.Length; i++)
        {
            // ��ȡ�����ļ�·��
            var path = AssetDatabase.GUIDToAssetPath(allAssetGUID[i]);
            validatePathList.Add(path);
            // �༭��������
            EditorUtility.DisplayProgressBar("����prefab", "Prefab:" + path, i * 1.0f / allAssetGUID.Length);
            if (AllFileDirContainPath(path)) Debug.LogError("AB���ñ����ļ�·�����ļ���·���ظ�!");
            else
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                // ��ȡָ��·���ļ�����������
                string[] alldps = AssetDatabase.GetDependencies(path);
                List<string> dpPathList = new List<string>();
                foreach (var dpPath in alldps)
                {
                    if (dpPath.EndsWith(".cs") || AllFileDirContainPath(dpPath)) continue; // ����������cs�ļ�  || prefab�е������� ��������ļ����д���(ͨ��·����������)
                    else
                    {
                        dpPathList.Add(dpPath);
                        allABFileList.Add(dpPath);
                    }
                }
                if (prefabDic.ContainsKey(go.name)) Debug.LogErrorFormat("������ͬ���ֵ�prefab! : {0}", go.name);
                else prefabDic.Add(go.name, dpPathList);
            }
        }
    }

    #endregion �ļ�����ɶ�Ӧ���ֵ�

    #region AB������

    /// <summary>
    ///  ���AssetBundle
    /// </summary>
    private static void BuildAssetBundle()
    {
        // ��ȡ����AB���
        string[] allBundNames = AssetDatabase.GetAllAssetBundleNames();
        // ·�� Ab���� �ֵ�
        Dictionary<string, string> pathABNameDic = new Dictionary<string, string>();

        for (int i = 0; i < allBundNames.Length; i++)
        {
            // ��ȡ�ñ�ǵ�������Դ·��
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(allBundNames[i]);
            for (int j = 0; j < assetPaths.Length; j++)
            {
                if (assetPaths[j].EndsWith("cs") || !IsValidatePath(assetPaths[j])) continue; // ȥ������Ч·����cs�ļ�
                pathABNameDic.Add(assetPaths[j], allBundNames[i]); // ������Դ��·�����д洢
            }
        }

        // ɾ���ı��AB��
        DelAssetBundle();

        // д�����ñ�
        WriteData(pathABNameDic);

        // API���
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(BUILDTARGETPATH, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null) Debug.LogError("AssetBundle ���ʧ�ܣ�");
        else Debug.Log("AssetBundle ������");
    }
    /// <summary>
    ///  ɾ���ı��AB��
    /// </summary>
    private static void DelAssetBundle()
    {
        string[] abLabels = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo dc = new DirectoryInfo(BUILDTARGETPATH);
        FileInfo[] files = dc.GetFiles("*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)
        {
            if (files[i].Name.EndsWith(".meta") || files[i].Name.EndsWith(".manifest") || ABLabelsContainName(files[i].Name, abLabels)) continue; // �˰��������õ�Ab��ǩ������meta�ļ�
            else
            {
                // ������ɾ��֮ǰ�����ð�
                if (File.Exists(files[i].FullName)) File.Delete(files[i].FullName);
                if (File.Exists(files[i].FullName + ".manifest")) File.Delete(files[i].FullName + ".manifest");
                if (File.Exists(files[i].FullName + ".meta")) File.Delete(files[i].FullName + ".meta");
                if (File.Exists(files[i].FullName + ".manifest.meta")) File.Delete(files[i].FullName + ".manifest.meta");
            }
        }
    }
    /// <summary>
    /// �жϸ�AB������ �Ƿ��ڴ��� ��ǰ��AB��ǩ��
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

    #endregion AB������

    #region AB����ǩ

    private static void SetABlabel(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null) Debug.LogErrorFormat("�����ڴ�·��! : {0}", path);
        else assetImporter.assetBundleName = name;
    }
    private static void SetABlabel(string name, List<string> pathList)
    {
        foreach (string path in pathList) SetABlabel(name, path);
    }
    /// <summary>
    /// �������úõ��ֵ� ���ļ����ļ��� ����AB��ǩ
    /// </summary>
    private static void SetABLabelByAllDic()
    {
        foreach (var item in fileDirDic) SetABlabel(item.Key, item.Value);
        foreach (var item in prefabDic) SetABlabel(item.Key, item.Value);
    }
    /// <summary>
    /// ���AB����ǩ �˴�ִ�к�༭����ǩΪ�հ�
    /// </summary>
    private static void ClearABLabel()
    {
        string[] allBdNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < allBdNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(allBdNames[i], true);
            EditorUtility.DisplayProgressBar("���AB��ǩ", "����:" + allBdNames[i], i * 1.0f / allBdNames.Length);
        }
    }

    #endregion AB����ǩ

    #region ͨ���жϷ���

    /// <summary>
    /// �ж�ָ��·���Ƿ��Ѿ��������ļ���·���� ���������޳�
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
    /// �Ƿ�����Ч·��(�ļ���·������Prefab·��)
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
    #endregion ͨ���жϷ���
}
