using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset"; // ab�����ñ�·��
    public static Dictionary<string, string> allFileDir = new Dictionary<string, string>(); // key: ab���� Vlaue:�ļ���·��

    [MenuItem("Tools/���")]
    public static void Build()
    {
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);

        HandleFileDir(cf);
        // �����ļ���

        /*   foreach (var item in cf.filePath)
           {
               Debug.Log(item);
           }*/
        HandleFile(cf);
    }
    // �����ļ��� ��ab�������ļ���·����Ӧ���д洢
    public static void HandleFileDir(ABConfig cf)
    {
        allFileDir.Clear();
        foreach (var item in cf.fileDirPath)
        {
            if (allFileDir.ContainsKey(item.ABName)) Debug.LogError("AB���������ظ�");
            else allFileDir.Add(item.ABName, item.Path);
        }

    }
    // �����ļ� ��ָ���ļ����µ��ļ�·�����д洢
    public static void HandleFile(ABConfig cf)
    {
        // ��ȡָ���ļ���·������Դ��GUID
        string[] allAssetGUID = AssetDatabase.FindAssets("t:prefab", cf.filePath.ToArray());
        // ����GUID��ȡ�ļ���·��
        for (int i = 0; i < allAssetGUID.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(allAssetGUID[i]);

            // �༭��������
            EditorUtility.DisplayProgressBar("����prefab", "Prefab:" + path, i * 1.0f / allAssetGUID.Length);
        }
        // ���������
        EditorUtility.ClearProgressBar();
    }
}
