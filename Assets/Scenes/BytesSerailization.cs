using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting;
using UnityEngine;

// ���л���
[System.Serializable]
public class BytesClass
{
    public string id;
    public string name;
    public List<int> list;
}

public class BytesSerailization : MonoBehaviour
{
    // ������
    public BytesClass CreateBytesClass()
    {
        BytesClass bc = new BytesClass();
        bc.id = "1";
        bc.name = "ITXCM";
        bc.list = new List<int>() { 1, 2 };
        return bc;
    }

    // ���������л��洢
    public void BytesClassSerailize(BytesClass bytesClass)
    {
        using FileStream fs = new FileStream(Application.dataPath + "/Bytes/test.bytes", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, bytesClass);
    }

    // �����Ʒ����л���ȡ
    public BytesClass BytesClassDeSerailize()
    {
        using FileStream fs = new FileStream(Application.dataPath + "/Bytes/test.bytes", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        return bf.Deserialize(fs) as BytesClass;
    }
}
