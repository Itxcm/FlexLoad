using System.Collections.Generic;
using System.Xml.Serialization;

/// <summary>
/// ��Ϊxml�洢����
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
    /// ȫ·�� ���ڼ���ָ��AssetBundle��Դ
    /// </summary>
    [XmlAttribute("Path")]
    public string Path { get; set; } // 

    /// <summary>
    /// ·����Ӧ��Crc �����ҵ�AB������AB��
    /// </summary>
    [XmlAttribute("Crc")]
    public uint Crc { get; set; } // 

    /// <summary>
    ///  AB����Ψһ��ʶ
    /// </summary>
    [XmlAttribute("ABName")]
    public string ABName { get; set; } //

    /// <summary>
    /// ��Դ����
    /// </summary>
    [XmlAttribute("AssetName")]
    public string AssetName { get; set; }

    /// <summary>
    /// ������AB�������б�
    /// </summary>
    [XmlElement("Dependence")]
    public List<string> Dependence { get; set; }
}