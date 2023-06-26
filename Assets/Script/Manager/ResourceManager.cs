using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    // �Ƿ��AssetBundle�м���
    public bool IsLoadFromAssetBundle = true;

    // ����ʹ�õ���Դ�ֵ� crc·����Ӧ��Դ
    public Dictionary<uint, ResourceItem> _assetDic = new Dictionary<uint, ResourceItem>();

    // δʹ�õ���ԴMap(���ü���Ϊ0) �ﵽ����������δʹ�õ�
    public DoubleLinkedMap<ResourceItem> resourceMap = new DoubleLinkedMap<ResourceItem>();

    #region ��ʵ������Դ�ļ�����ж��

    /// <summary>
    /// ͬ������
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

        // ��Դ����
        if (item != null)
        {
            return item.Object as T;
        }

        // ��Դ������
        T obj = null;

        // �༭ģʽ�� ����AB���м���
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

        // �Ǳ༭������ ��AB���м���

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
        // ������Դ
        CacheResource(crc, path, ref item, obj);

        return obj;
    }
    /// <summary>
    ///  ��Դж��
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="isReleaseFromMemory">�Ƿ���ڴ��ͷ�</param>
    /// <returns></returns>
    public bool ReleaseResource(Object obj, bool isReleaseFromMemory)
    {
        if (obj == null)
        {
            return false;
        }

        ResourceItem item = _assetDic.Values.FirstOrDefault(item => item.GUID == obj.GetInstanceID());

        // ��Դ�ֵ䲻���ڸ���Դ
        if (item == null)
        {
            Debug.LogErrorFormat("_assetDic not contains this Object, Name:{0}", obj.name);
            return false;
        }
        // ȥ�������� ���ո���Դ
        item.RefCount--;
        RecycleResource(item, isReleaseFromMemory);
        return true;
    }
    #endregion ��ʵ������Դ�ļ�����ж��

    /// <summary>
    /// ������Դ ���ͷŻ����²��뵽ͷ��
    /// </summary>
    /// <param name="item"></param>
    /// <param name="isReleaseFromMemory">�Ƿ���ڴ��ͷ�</param>
    public void RecycleResource(ResourceItem item, bool isReleaseFromMemory = false)
    {
        if (item == null && item.RefCount > 0)
        {
            return;
        }
        // ����Ҫ���ڴ��ͷ�
        if (!isReleaseFromMemory)
        {
            resourceMap.Insert(item);
            return;
        }

        // �ͷ���Դ
        AssetBundleManager.Instance.ReleaseResourceItem(item);
        if (item.Object != null)
        {
            item.Object = null;
        }

    }
    /// <summary>
    /// �������õ���Դ
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

        // ��ӵ����û�����ֵ�
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
    /// ����Crc·����ȡָ��������Դ
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
    /// �༭��ģʽ�� ����ָ��·����Դ
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