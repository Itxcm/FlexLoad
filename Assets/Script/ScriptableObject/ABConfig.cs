using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject
{
    // �����ļ�·���б�(Prefabs) ���뱣֤����Ψһ�� : ÿ��·���ļ���һ����
    public List<string> filePath = new List<string>();

    // �ļ���·���б� ����б��е������ļ��� : ÿ���ļ��д�һ���� ָ������
    public List<FileDirABName> fileDirPath = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName
    {
        public string ABName;
        public string Path;
    }
}