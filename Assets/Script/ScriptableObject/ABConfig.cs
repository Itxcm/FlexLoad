using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject
{
    // �����ļ����µ�ÿ��Prefab·�� : ÿ���ļ���һ����
    public List<string> prefabPathList = new List<string>();

    // ָ���ļ���·�������ƴ�� : ÿ���ļ��д�һ����
    public List<FileDirABName> fileDirPathList = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName
    {
        public string ABName;
        public string Path;
    }
}