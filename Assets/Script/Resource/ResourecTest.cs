using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class ResourecTest : MonoBehaviour
{

    void Start()
    {
        AssetBundle abBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/Data");
        TextAsset textAsset = abBundle.LoadAsset<TextAsset>("AssetBundleConfig.bytes");
        using MemoryStream ms = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig cf = bf.Deserialize(ms) as AssetBundleConfig;
        string targetPath = "Assets/GameData/Prefabs/Attack.prefab";
        uint crc = Crc32.GetCrc32(targetPath);
        ABBase abBase = null;
        foreach (ABBase item in cf.ABList)
        {
            if (item.Crc == crc) abBase = item;
        }
        // ����������
        foreach (var dp in abBase.ABDependence)
        {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + dp);
        }

        // ��������

        AssetBundle bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
        GameObject go = bundle.LoadAsset<GameObject>(abBase.AssetName);
        Instantiate(go);
    }


}
