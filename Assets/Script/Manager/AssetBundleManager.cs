using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    // AB资源字典 key为Crc路径
    protected Dictionary<uint, ResourceItem> pathResoucrItemDic = new Dictionary<uint, ResourceItem>();
    // AssetBundle资源字典 key为AB包名的Crc
    protected Dictionary<uint, AssetBundleItem> pathAssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();

    #region 对象池声明
    // AssetBundleItem 的对象池
    protected ClassObjectPool<AssetBundleItem> assetBundleItemPool = ObjectManager.Instance.GetOrCreateClassObjectPool<AssetBundleItem>(500);
    #endregion 对象池声明

    /// <summary>
    /// 加载AB包配置 并将配置的ABBase项转成ResourceItem存储
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig()
    {
        pathResoucrItemDic.Clear();

        AssetBundle dataBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/data");
        TextAsset textAsset = dataBundle.LoadAsset<TextAsset>("AssetBundleConfig.bytes");
        if (textAsset == null)
        {
            Debug.LogError("Data的Bundle中没有AssetBundleConfig资源");
            return false;
        }

        using MemoryStream ms = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig cg = bf.Deserialize(ms) as AssetBundleConfig;

        for (int i = 0; i < cg.ABList.Count; i++)
        {
            ABBase aBBase = cg.ABList[i];
            ResourceItem item = new ResourceItem();
            item.Crc = aBBase.Crc;
            item.ABName = aBBase.ABName;
            item.AssetName = aBBase.AssetName;
            item.Dependce = aBBase.Dependence;

            if (pathResoucrItemDic.ContainsKey(item.Crc))
            {
                Debug.LogErrorFormat("重复的Crc路径! 资源名:{0} AB包名:{1}", item.AssetName, item.ABName);
            }
            else pathResoucrItemDic.Add(item.Crc, item);
        }

        return true;
    }
    /// <summary>
    ///  根据Crc路径获取ResourceItem资源
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem GetResourceItem(uint crc)
    {
        if (!pathResoucrItemDic.TryGetValue(crc, out ResourceItem item) || item == null)
        {
            Debug.LogErrorFormat("AB配置表中不存在这个资源 路径Crc为:{0}", crc);
        }
        if (item.AssetBundle != null)
        {
            return item;
        }

        // 先加载依赖
        if (item.Dependce != null)
        {
            for (int i = 0; i < item.Dependce.Count; i++) LoadAsstBundle(item.Dependce[i]);
        }

        item.AssetBundle = LoadAsstBundle(item.ABName);

        return item;
    }
    /// <summary>
    ///  根据AB包名加载单个Assetbundle 重复加载添加引用个数
    /// </summary>
    /// <param name="abName">ab包名</param>
    /// <returns></returns>
    private AssetBundle LoadAsstBundle(string abName)
    {
        uint crc = Crc32.GetCrc32(abName);

        // 从资源字典查询有没有这个Assebundle
        if (!pathAssetBundleItemDic.TryGetValue(crc, out AssetBundleItem assetBundleItem))
        {
            // 加载AssetBundle
            AssetBundle assetBundle = null;
            string path = Application.streamingAssetsPath + "/" + abName;
            if (File.Exists(path)) assetBundle = AssetBundle.LoadFromFile(path);
            else Debug.LogErrorFormat("此AssetBundle不存在 路径:{0}", path);
            if (assetBundle == null) Debug.LogErrorFormat("加载Assetbundle失败 路径:{0}", path);

            // 从池中取出AssetBundleItem赋值
            assetBundleItem = assetBundleItemPool.Spawn(true);
            assetBundleItem.AssetBundle = assetBundle;
            assetBundleItem.RefCount++;

            // 添加到AssetBundleItem字典
            pathAssetBundleItemDic.Add(crc, assetBundleItem);
        }
        else assetBundleItem.RefCount++;

        return assetBundleItem.AssetBundle;
    }
}

/// <summary>
///  记录当前每个Bundle的引用 解决加载卸载重复
/// </summary>
public class AssetBundleItem
{
    public AssetBundle AssetBundle;
    public int RefCount;

    public void Reset()
    {
        AssetBundle = null;
        RefCount = 0;
    }
}

/// <summary>
/// AB配置表中资源Item 类似ABBase
/// </summary>
public class ResourceItem
{
    public uint Crc; // 路径对应Crc
    public string ABName; //  AB包名
    public string AssetName; // 资源名称
    public List<string> Dependce; // 依赖列表
    public AssetBundle AssetBundle; // 加载完成的AssetBundle
}