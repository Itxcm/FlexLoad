using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
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

    private void Start()
    {
        /*  AssetBundleConfig cf = BytesClassDeSerailize();
          foreach (var item in cf.ABList)
          {
              Debug.Log(item.Path);
          }*/
    }
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
    /*    public AssetBundleConfig BytesClassDeSerailize()
        {
            //  using FileStream fs = new FileStream(Application.dataPath + "/Bytes/test.bytes", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            TextAsset ts = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Config/AssetBundleConfig.bytes");
            MemoryStream ms = new MemoryStream(ts.bytes);
            BinaryFormatter bf = new BinaryFormatter();
            return bf.Deserialize(ms) as AssetBundleConfig;
        }*/
}
