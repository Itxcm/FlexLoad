
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
    protected Dictionary<uint, AssetBundleItem> abNameAssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();

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
    /// 加载ResourceItem资源的AssetBundle
    /// </summary>
    /// <param name="crc">资源Crc路径</param>
    /// <returns></returns>
    public ResourceItem LoadResourceItem(uint crc)
    {
        if (!pathResoucrItemDic.TryGetValue(crc, out ResourceItem item) || item == null)
        {
            Debug.LogErrorFormat("AB资源字典中不存在这个资源 路径Crc为:{0}", crc);
        }
        if (item.AssetBundle != null)
        {
            return item;
        }

        // 先加载依赖
        if (item.Dependce != null)
        {
            for (int i = 0; i < item.Dependce.Count; i++) LoadAssetBundle(item.Dependce[i]);
        }

        item.AssetBundle = LoadAssetBundle(item.ABName);

        return item;
    }
    /// <summary>
    /// 释放ResourceItem的AssetBundle
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseResourceItem(ResourceItem item)
    {
        if (item == null) return;

        // 先卸载
        if (item.Dependce != null)
        {
            for (int i = 0; i < item.Dependce.Count; i++) UnLoadAssetBundle(item.Dependce[i]);
        }

        UnLoadAssetBundle(item.ABName);
    }
    /// <summary>
    /// 根据Crc路径获取ResourceItem
    /// </summary>
    /// <param name="crc">crc路径</param>
    /// <returns></returns>
    public ResourceItem GetResourceByCrcPath(uint crc) => pathResoucrItemDic[crc];
    /// <summary>
    ///  根据AB包名加载单个Assetbundle 重复加载添加引用个数
    /// </summary>
    /// <param name="abName">ab包名</param>
    /// <returns></returns>
    private AssetBundle LoadAssetBundle(string abName)
    {
        uint crc = Crc32.GetCrc32(abName);

        // 从资源字典查询有没有这个Assebundle
        if (!abNameAssetBundleItemDic.TryGetValue(crc, out AssetBundleItem assetBundleItem))
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
            abNameAssetBundleItemDic.Add(crc, assetBundleItem);
        }
        else assetBundleItem.RefCount++;

        return assetBundleItem.AssetBundle;
    }
    /// <summary>
    /// 根据AB包名卸载单个Assetbundle 存在其他引用则只减少引用次数
    /// </summary>
    /// <param name="abName"></param>
    private void UnLoadAssetBundle(string abName)
    {
        uint crc = Crc32.GetCrc32(abName);
        if (abNameAssetBundleItemDic.TryGetValue(crc, out AssetBundleItem item) && item != null)
        {
            item.RefCount--;
            if (item.RefCount <= 0 && item.AssetBundle != null)
            {
                item.AssetBundle.Unload(true);
                item.Reset();
                assetBundleItemPool.Recyle(item);
                abNameAssetBundleItemDic.Remove(crc);
            }
        }
    }
}

/// <summary>
///  记录当前每个Bundle的引用 解决加载卸载重复
/// </summary>
public class AssetBundleItem
{
    public AssetBundle AssetBundle = null;
    public int RefCount = 0;

    /// <summary>
    /// 卸载时滞空
    /// </summary>
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
    public int GUID = 0; // 资源唯一标识
    public uint Crc = 0; // 路径对应Crc
    public string ABName = string.Empty; //  AB包名
    public string AssetName = string.Empty; // 资源名称
    public List<string> Dependce = null; // 依赖列表
    public AssetBundle AssetBundle = null; // 加载完成的AssetBundle
    public Object Object = null; // 实例化生成的游戏对象
    public float LastRefTime = 0.0f; // 最后引用时间
    protected int _refCount = 0;
    public int RefCount // 引用计数
    {
        get => _refCount;
        set
        {
            _refCount = value;
            if (_refCount < 0)
            {
                Debug.LogErrorFormat("资源引用计数错误 资源名称{0}", AssetName);
            }
        }
    }
}