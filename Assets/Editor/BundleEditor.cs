using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset"; // ab�����ñ�·��
    public static List<string> allFileList = new List<string>(); // ����AB���ļ���·�� ��Ҫ���˵��б�
    public static Dictionary<string, List<string>> prefabDic = new Dictionary<string, List<string>>(); // ����prefab�ֵ� ab���� : ����·���б�
    public static Dictionary<string, string> fileDirDic = new Dictionary<string, string>(); // �����ļ���ab���ֵ� ab���� : ·�� 

    [MenuItem("Tools/���")]
    public static void Build()
    {
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        // ������Ҫ�����
        HandleFileDir(cf);
        HandlePrfab(cf);

        // ����AB��ǩ
        SetABLabelByAllDic();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        //  ClearABLabel();



        // ���������
        EditorUtility.ClearProgressBar();
    }
    // �����ļ��� ���ļ���·�� �� ָ������ ��Ϊ�ֵ�洢
    public static void HandleFileDir(ABConfig cf)
    {
        prefabDic.Clear();
        fileDirDic.Clear();
        allFileList.Clear();
        foreach (var item in cf.fileDirPathList)
        {
            if (fileDirDic.ContainsKey(item.ABName)) Debug.LogError("AB���ñ��ļ���AB���������ظ�!");
            else
            {
                fileDirDic.Add(item.ABName, item.Path);
                allFileList.Add(item.Path);
            }
        }
    }
    // �����ļ� ���ļ�����ÿһ��prefabs �� ���� �� ����·���б� ��Ϊ�ֵ�洢
    public static void HandlePrfab(ABConfig cf)
    {
        // ��ȡָ���ļ���·������Դ��GUID
        string[] allAssetGUID = AssetDatabase.FindAssets("t:prefab", cf.prefabPathList.ToArray());
        // ����GUID��ȡ�ļ�·��
        for (int i = 0; i < allAssetGUID.Length; i++)
        {
            // ��ȡ�����ļ�·��
            var path = AssetDatabase.GUIDToAssetPath(allAssetGUID[i]);
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
                    if (dpPath.EndsWith(".cs")) continue; // ����������cs�ļ�
                    if (AllFileDirContainPath(dpPath)) Debug.LogErrorFormat("Prefab�������ļ�·�����ļ���·���ظ�! : {0}", dpPath);
                    else
                    {
                        dpPathList.Add(dpPath);
                        allFileList.Add(dpPath);
                    }
                }
                if (prefabDic.ContainsKey(go.name)) Debug.LogErrorFormat("������ͬ���ֵ�prefab! : {0}", go.name);
                else prefabDic.Add(go.name, dpPathList);
            }
        }
    }
    // �������úõ��ֵ� ���ļ����ļ��� ����AB��ǩ
    public static void SetABLabelByAllDic()
    {
        foreach (var item in prefabDic) SetABlabel(item.Key, item.Value);
        foreach (var item in fileDirDic) SetABlabel(item.Key, item.Value);
    }
    // ���AB����ǩ �˴�ִ�к�༭����ǩΪ�հ�
    public static void ClearABLabel()
    {
        string[] allBdNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < allBdNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(allBdNames[i], true);
            EditorUtility.DisplayProgressBar("���AB��ǩ", "����:" + allBdNames[i], i * 1.0f / allBdNames.Length);
        }
    }

    #region AB����ǩ
    public static void SetABlabel(string name, string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null) Debug.LogErrorFormat("�����ڴ�·��! : {0}", path);
        else assetImporter.assetBundleName = name;
    }
    public static void SetABlabel(string name, List<string> pathList)
    {
        foreach (string path in pathList) SetABlabel(name, path);
    }
    #endregion AB����ǩ

    #region ͨ���жϷ���
    // �ж�ָ��·���Ƿ��Ѿ��������ļ���·����
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
