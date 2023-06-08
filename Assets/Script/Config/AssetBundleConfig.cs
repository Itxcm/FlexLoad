using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// 作为xml存储的类
/// </summary>

[System.Serializable]
public class AssetBundleConfig
{
    [XmlElement("ABList")]
    public List<ABBase> ABList { get; set; }
}

[System.Serializable]
public class ABBase
{
    /// <summary>
    /// 全路径 用于加载指定AssetBundle资源
    /// </summary>
    [XmlAttribute("Path")]
    public string Path { get; set; } // 

    /// <summary>
    /// 路径对应的Crc 用于找到AB包名和AB包
    /// </summary>
    [XmlAttribute("Crc")]
    public uint Crc { get; set; } // 

    /// <summary>
    ///  AB包名唯一标识
    /// </summary>
    [XmlAttribute("ABName")]
    public string ABName { get; set; } //

    /// <summary>
    /// 资源名称
    /// </summary>
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; }

    /// <summary>
    /// 依赖的AB包名称列表
    /// </summary>
    [XmlElement("Dependence")]
    public List<string> Dependence { get; set; }
}