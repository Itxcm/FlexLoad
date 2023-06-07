using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
    public static string ABCONFIGPATH = "Assets/Data/ABConfig.asset";

    [MenuItem("Tools/´ò°ü")]
    public static void Build()
    {
        ABConfig cf = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH);


        foreach (var item in cf.filePath)
        {
            Debug.Log(item);
        }

        foreach (var item in cf.fileDirPath)
        {
            Debug.Log(item.ABName);
            Debug.Log(item.Path);
        }
    }
}
