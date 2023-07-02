using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ResourceManager;

public class ResourecTest : MonoBehaviour
{
    AudioSource m_AudioSource;
    AudioClip Clip;
    public Image IMG;

    private void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
        AssetBundleManager.Instance.LoadAssetBundleConfig();

        ResourceManager.Instance.Init(this);
    }
    void Start()
    {
        /*   Clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
           m_AudioSource.clip = Clip;
           m_AudioSource.Play();*/
        ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3", true);

        //  uint crc = Crc32.GetCrc32("Assets/GameData/Sounds/senlin.mp3");
        /*    ResourceManager.Instance.LoadResourceAsync("Assets/GameData/Sounds/senlin.mp3", AsyncLoadPriority.RES_SLOW, false, (path, obj, objs) =>
            {
                Clip = obj as AudioClip;
                m_AudioSource.clip = Clip;
                m_AudioSource.Play();
            });*/


        //   IMG.overrideSprite = ResourceManager.Instance.LoadResource<Sprite>("Assets/GameData/Sprite/WeiPai.png");


        /*
                ResourceManager.Instance.LoadResourceAsync("Assets/GameData/Sprite/WeiPai.png", 0, AsyncLoadPriority.RES_SLOW, true
                    , (path, obj, objs) =>
                {
                    IMG.overrideSprite = obj as Sprite;
                });*/


        /*  ABBase aBBase1 = new ABBase();
          aBBase1.ABName = "Test1";
          ABBase aBBase2 = new ABBase();
          aBBase2.ABName = "Test2";
          ABBase aBBase3 = new ABBase();
          aBBase3.ABName = "Test3";

          DoubleLinkedMap<ABBase> doubleLinkedMap = new DoubleLinkedMap<ABBase>();

          doubleLinkedMap.Insert(aBBase1);
          doubleLinkedMap.Insert(aBBase2);
          doubleLinkedMap.Insert(aBBase3);
          doubleLinkedMap.Move(aBBase2);*/



        /*  AssetBundleManager.Instance.LoadAssetBundleConfig();

          uint crc = Crc32.GetCrc32("Assets/GameData/Prefabs/Attack.prefab");
          ResourceItem item = AssetBundleManager.Instance.LoadResourceItem(crc);
          GameObject go = item.AssetBundle.LoadAsset<GameObject>(item.AssetName);
          Instantiate(go);*/

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
           // º”‘ÿ“¿¿µœÓ
           foreach (var dp in abBase.Dependence)
           {
               AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + dp);
           }

           // º”‘ÿ◊‘…Ì

           AssetBundle bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
           GameObject go = bundle.LoadAsset<GameObject>(abBase.AssetName);
           Instantiate(go);*/
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            m_AudioSource.Stop();
            m_AudioSource.clip = null;
            ResourceManager.Instance.ReleaseResource(Clip, true);
            Clip = null;
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            long start = System.DateTime.Now.Ticks;
            Clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/GameData/Sounds/senlin.mp3");
            Debug.Log($"{System.DateTime.Now.Ticks - start}");
            m_AudioSource.clip = Clip;
            m_AudioSource.Play();
        }
    }
    private void OnApplicationQuit()
    {
#if UNITY_EDITOR
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
        Debug.Log("«Âø’±‡º≠∆˜ª∫¥Ê");
#endif
    }
}
