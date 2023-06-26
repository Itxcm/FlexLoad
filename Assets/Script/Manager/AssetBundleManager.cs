
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    // AB��Դ�ֵ� keyΪCrc·��
    protected Dictionary<uint, ResourceItem> pathResoucrItemDic = new Dictionary<uint, ResourceItem>();
    // AssetBundle��Դ�ֵ� keyΪAB������Crc
    protected Dictionary<uint, AssetBundleItem> abNameAssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();

    #region ���������
    // AssetBundleItem �Ķ����
    protected ClassObjectPool<AssetBundleItem> assetBundleItemPool = ObjectManager.Instance.GetOrCreateClassObjectPool<AssetBundleItem>(500);
    #endregion ���������

    /// <summary>
    /// ����AB������ �������õ�ABBase��ת��ResourceItem�洢
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig()
    {
        pathResoucrItemDic.Clear();

        AssetBundle dataBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/data");
        TextAsset textAsset = dataBundle.LoadAsset<TextAsset>("AssetBundleConfig.bytes");
        if (textAsset == null)
        {
            Debug.LogError("Data��Bundle��û��AssetBundleConfig��Դ");
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
                Debug.LogErrorFormat("�ظ���Crc·��! ��Դ��:{0} AB����:{1}", item.AssetName, item.ABName);
            }
            else pathResoucrItemDic.Add(item.Crc, item);
        }

        return true;
    }
    /// <summary>
    /// ����ResourceItem��Դ��AssetBundle
    /// </summary>
    /// <param name="crc">��ԴCrc·��</param>
    /// <returns></returns>
    public ResourceItem LoadResourceItem(uint crc)
    {
        if (!pathResoucrItemDic.TryGetValue(crc, out ResourceItem item) || item == null)
        {
            Debug.LogErrorFormat("AB��Դ�ֵ��в����������Դ ·��CrcΪ:{0}", crc);
        }
        if (item.AssetBundle != null)
        {
            return item;
        }

        // �ȼ�������
        if (item.Dependce != null)
        {
            for (int i = 0; i < item.Dependce.Count; i++) LoadAssetBundle(item.Dependce[i]);
        }

        item.AssetBundle = LoadAssetBundle(item.ABName);

        return item;
    }
    /// <summary>
    /// �ͷ�ResourceItem��AssetBundle
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseResourceItem(ResourceItem item)
    {
        if (item == null) return;

        // ��ж��
        if (item.Dependce != null)
        {
            for (int i = 0; i < item.Dependce.Count; i++) UnLoadAssetBundle(item.Dependce[i]);
        }

        UnLoadAssetBundle(item.ABName);
    }
    /// <summary>
    /// ����Crc·����ȡResourceItem
    /// </summary>
    /// <param name="crc">crc·��</param>
    /// <returns></returns>
    public ResourceItem GetResourceByCrcPath(uint crc) => pathResoucrItemDic[crc];
    /// <summary>
    ///  ����AB�������ص���Assetbundle �ظ�����������ø���
    /// </summary>
    /// <param name="abName">ab����</param>
    /// <returns></returns>
    private AssetBundle LoadAssetBundle(string abName)
    {
        uint crc = Crc32.GetCrc32(abName);

        // ����Դ�ֵ��ѯ��û�����Assebundle
        if (!abNameAssetBundleItemDic.TryGetValue(crc, out AssetBundleItem assetBundleItem))
        {
            // ����AssetBundle
            AssetBundle assetBundle = null;
            string path = Application.streamingAssetsPath + "/" + abName;
            if (File.Exists(path)) assetBundle = AssetBundle.LoadFromFile(path);
            else Debug.LogErrorFormat("��AssetBundle������ ·��:{0}", path);
            if (assetBundle == null) Debug.LogErrorFormat("����Assetbundleʧ�� ·��:{0}", path);

            // �ӳ���ȡ��AssetBundleItem��ֵ
            assetBundleItem = assetBundleItemPool.Spawn(true);
            assetBundleItem.AssetBundle = assetBundle;
            assetBundleItem.RefCount++;

            // ��ӵ�AssetBundleItem�ֵ�
            abNameAssetBundleItemDic.Add(crc, assetBundleItem);
        }
        else assetBundleItem.RefCount++;

        return assetBundleItem.AssetBundle;
    }
    /// <summary>
    /// ����AB����ж�ص���Assetbundle ��������������ֻ�������ô���
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
///  ��¼��ǰÿ��Bundle������ �������ж���ظ�
/// </summary>
public class AssetBundleItem
{
    public AssetBundle AssetBundle = null;
    public int RefCount = 0;

    /// <summary>
    /// ж��ʱ�Ϳ�
    /// </summary>
    public void Reset()
    {
        AssetBundle = null;
        RefCount = 0;
    }
}

/// <summary>
/// AB���ñ�����ԴItem ����ABBase
/// </summary>
public class ResourceItem
{
    public int GUID = 0; // ��ԴΨһ��ʶ
    public uint Crc = 0; // ·����ӦCrc
    public string ABName = string.Empty; //  AB����
    public string AssetName = string.Empty; // ��Դ����
    public List<string> Dependce = null; // �����б�
    public AssetBundle AssetBundle = null; // ������ɵ�AssetBundle
    public Object Object = null; // ʵ�������ɵ���Ϸ����
    public float LastRefTime = 0.0f; // �������ʱ��
    protected int _refCount = 0;
    public int RefCount // ���ü���
    {
        get => _refCount;
        set
        {
            _refCount = value;
            if (_refCount < 0)
            {
                Debug.LogErrorFormat("��Դ���ü������� ��Դ����{0}", AssetName);
            }
        }
    }
}