using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;
using System.Xml;

// 定义一个可序列化成Xml文件的类
[System.Serializable]
public class XmlClass
{
    [XmlAttribute("id")]
    public string id;
    [XmlAttribute("name")]
    public string name;
    [XmlElement("list")]
    public List<int> list;
}

public class XmlSerialization : MonoBehaviour
{

    // 创建一个可序列化的Xml类
    public XmlClass CreateXmlClass()
    {
        XmlClass xmlClass = new XmlClass();
        xmlClass.id = "1";
        xmlClass.name = "ITXCM";
        xmlClass.list = new List<int>() { 1, 2 };
        return xmlClass;
    }

    // 将Xml类序列化存储到本地
    public void XmlClassSerialize(XmlClass xmlClass)
    {
        using FileStream fs = new FileStream(Application.dataPath + "/Xml/test.xml", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using StreamWriter sw = new StreamWriter(fs);
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlClass));
        xmlSerializer.Serialize(sw, xmlClass);
    }

    // 将类反序列化进行读取
    public XmlClass XmlClassDeSerialize()
    {
        using FileStream fs = new FileStream(Application.dataPath + "/Xml/test.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlClass));
        return xmlSerializer.Deserialize(fs) as XmlClass;
    }

}
