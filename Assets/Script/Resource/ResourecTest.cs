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

        AssetBundleManager.Instance.LoadAssetBundleConfig();

        uint crc = Crc32.GetCrc32("Assets/GameData/Prefabs/Attack.prefab");
        ResourceItem item = AssetBundleManager.Instance.LoadResourceItem(crc);
        GameObject go = item.AssetBundle.LoadAsset<GameObject>(item.AssetName);
        Instantiate(go);
        /* AssetBundleManager.Instance.ReleaseResourceItem(item);
         Instantiate(go);*/
        //  ResourceItem item1 = AssetBundleManager.Instance.GetResourceItem(crc1);

        /*   AssetBundle abBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/Data");
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
           // 加载依赖项
           foreach (var dp in abBase.Dependence)
           {
               AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + dp);
           }

           // 加载自身

           AssetBundle bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
           GameObject go = bundle.LoadAsset<GameObject>(abBase.AssetName);
           Instantiate(go);*/
    }
}
