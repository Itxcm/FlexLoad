using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using System.IO;
using System.Xml;

// ����һ�������л���Xml�ļ�����
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

    // ����һ�������л���Xml��
    public XmlClass CreateXmlClass()
    {
        XmlClass xmlClass = new XmlClass();
        xmlClass.id = "1";
        xmlClass.name = "ITXCM";
        xmlClass.list = new List<int>() { 1, 2 };
        return xmlClass;
    }

    // ��Xml�����л��洢������
    public void XmlClassSerialize(XmlClass xmlClass)
    {
        using FileStream fs = new FileStream(Application.dataPath + "/Xml/test.xml", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using StreamWriter sw = new StreamWriter(fs);
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlClass));
        xmlSerializer.Serialize(sw, xmlClass);
    }

    // ���෴���л����ж�ȡ
    public XmlClass XmlClassDeSerialize()
    {
        using FileStream fs = new FileStream(Application.dataPath + "/Xml/test.xml", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlClass));
        return xmlSerializer.Deserialize(fs) as XmlClass;
    }

}
