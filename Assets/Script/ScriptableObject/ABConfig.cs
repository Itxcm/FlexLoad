using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject
{
    // 单个文件路径列表(Prefabs) 必须保证名字唯一性 : 每个路径文件打一个包
    public List<string> filePath = new List<string>();

    // 文件夹路径列表 打包列表中的所有文件夹 : 每个文件夹打一个包 指定包名
    public List<FileDirABName> fileDirPath = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName
    {
        public string ABName;
        public string Path;
    }
}