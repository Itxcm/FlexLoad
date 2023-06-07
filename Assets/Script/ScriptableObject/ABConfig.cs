using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject
{
    // 单个文件夹下的每个Prefab路径 : 每个文件打一个包
    public List<string> prefabPathList = new List<string>();

    // 指定文件夹路径和名称打包 : 每个文件夹打一个包
    public List<FileDirABName> fileDirPathList = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName
    {
        public string ABName;
        public string Path;
    }
}