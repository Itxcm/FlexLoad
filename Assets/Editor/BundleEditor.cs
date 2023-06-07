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
        HandleFileDir(cf);
        HandlePrfab(cf);
    }
    // �����ļ��� ��ab�������ļ���·����Ӧ���д洢
    public static void HandleFileDir(ABConfig cf)
    {
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
    // �����ļ� ��ָ���ļ����µ��ļ�·�����д洢
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
                    if (AllFileDirContainPath(dpPath) || !dpPath.EndsWith(".cs")) Debug.LogErrorFormat("Prefab�������ļ�·�����ļ���·���ظ�! : {0}", dpPath);
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
        // ���������
        EditorUtility.ClearProgressBar();
    }

    // �ж�ָ��·���Ƿ��Ѿ��������ļ���·����
    public static bool AllFileDirContainPath(string path)
    {
        for (int i = 0; i < allFileList.Count; i++)
        {
            if (path == allFileList[i] || allFileList.Contains(path)) return true;
        }
        return false;
    }
}
