using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    // 是否从AssetBundle中加载
    public bool IsLoadFromAssetBundle = true;

    // 正在使用的资源字典 crc路径对应资源
    public Dictionary<uint, ResourceItem> _assetDic = new Dictionary<uint, ResourceItem>();

    // 未使用的资源Map(引用计数为0) 达到最大清除最早未使用的
    public DoubleLinkedMap<ResourceItem> resourceMap = new DoubleLinkedMap<ResourceItem>();

    #region 非实例化资源的加载与卸载

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        uint crc = Crc32.GetCrc32(path);
        ResourceItem item = GetCacheResource(crc);

        // 资源存在
        if (item != null)
        {
            return item.Object as T;
        }

        // 资源不存在
        T obj = null;

        // 编辑模式中 不从AB包中加载
#if UNITY_EDITOR
        if (!IsLoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.GetResourceByCrcPath(crc);
            if (item.Object != null)
            {
                obj = item.Object as T;
            }
            else
            {
                obj = LoadAssetByEditor<T>(path);
            }
        }
#endif

        // 非编辑器环境 从AB包中加载

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceItem(crc);
            if (item != null && item.AssetBundle != null)
            {
                if (item.Object != null)
                {
                    obj = item.Object as T;
                }
                else
                {
                    obj = item.AssetBundle.LoadAsset<T>(item.AssetName);
                }

            }
        }
        // 缓存资源
        CacheResource(crc, path, ref item, obj);

        return obj;
    }
    /// <summary>
    ///  资源卸载
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="isReleaseFromMemory">是否从内存释放</param>
    /// <returns></returns>
    public bool ReleaseResource(Object obj, bool isReleaseFromMemory)
    {
        if (obj == null)
        {
            return false;
        }

        ResourceItem item = _assetDic.Values.FirstOrDefault(item => item.GUID == obj.GetInstanceID());

        // 资源字典不存在该资源
        if (item == null)
        {
            Debug.LogErrorFormat("_assetDic not contains this Object, Name:{0}", obj.name);
            return false;
        }
        // 去除该引用 回收该资源
        item.RefCount--;
        RecycleResource(item, isReleaseFromMemory);
        return true;
    }
    #endregion 非实例化资源的加载与卸载

    /// <summary>
    /// 回收资源 不释放会重新插入到头部
    /// </summary>
    /// <param name="item"></param>
    /// <param name="isReleaseFromMemory">是否从内存释放</param>
    public void RecycleResource(ResourceItem item, bool isReleaseFromMemory = false)
    {
        if (item == null && item.RefCount > 0)
        {
            return;
        }
        // 不需要从内存释放
        if (!isReleaseFromMemory)
        {
            resourceMap.Insert(item);
            return;
        }

        // 释放资源
        AssetBundleManager.Instance.ReleaseResourceItem(item);
        if (item.Object != null)
        {
            item.Object = null;
        }

    }
    /// <summary>
    /// 缓存引用的资源
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="path"></param>
    /// <param name="item"></param>
    /// <param name="obj"></param>
    /// <param name="addRefCount"></param>
    private void CacheResource(uint crc, string path, ref ResourceItem item, Object obj, int addRefCount = 1)
    {
        if (item == null)
        {
            Debug.LogErrorFormat("ResourceItem is null Path:{0}", path);
        }
        if (obj == null)
        {
            Debug.LogErrorFormat("Object is not be load Path:{0}", path);
        }

        item.RefCount += addRefCount;
        item.LastRefTime = Time.realtimeSinceStartup;
        item.Object = obj;
        item.GUID = obj.GetInstanceID();

        // 添加到引用缓存的字典
        if (_assetDic.TryGetValue(crc, out ResourceItem oldItem))
        {
            _assetDic[crc] = item;
        }
        else
        {
            _assetDic.Add(crc, item);
        }
    }
    /// <summary>
    /// 根据Crc路径获取指定缓存资源
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="refCount"></param>
    /// <returns></returns>
    private ResourceItem GetCacheResource(uint crc, int refCount = 1)
    {
        if (_assetDic.TryGetValue(crc, out ResourceItem item) && item != null)
        {
            item.RefCount += refCount;
            item.LastRefTime = Time.realtimeSinceStartup;
        }
        return item;
    }

#if UNITY_EDITOR

    /// <summary>
    /// 编辑器模式下 加载指定路径资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadAssetByEditor<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

#endif
}