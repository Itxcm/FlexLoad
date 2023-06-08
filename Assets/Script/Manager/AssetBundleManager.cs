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
    protected Dictionary<uint, AssetBundleItem> pathAssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();

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
    ///  ����Crc·����ȡResourceItem��Դ
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem GetResourceItem(uint crc)
    {
        if (!pathResoucrItemDic.TryGetValue(crc, out ResourceItem item) || item == null)
        {
            Debug.LogErrorFormat("AB���ñ��в����������Դ ·��CrcΪ:{0}", crc);
        }
        if (item.AssetBundle != null)
        {
            return item;
        }

        // �ȼ�������
        if (item.Dependce != null)
        {
            for (int i = 0; i < item.Dependce.Count; i++) LoadAsstBundle(item.Dependce[i]);
        }

        item.AssetBundle = LoadAsstBundle(item.ABName);

        return item;
    }
    /// <summary>
    ///  ����AB�������ص���Assetbundle �ظ�����������ø���
    /// </summary>
    /// <param name="abName">ab����</param>
    /// <returns></returns>
    private AssetBundle LoadAsstBundle(string abName)
    {
        uint crc = Crc32.GetCrc32(abName);

        // ����Դ�ֵ��ѯ��û�����Assebundle
        if (!pathAssetBundleItemDic.TryGetValue(crc, out AssetBundleItem assetBundleItem))
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
            pathAssetBundleItemDic.Add(crc, assetBundleItem);
        }
        else assetBundleItem.RefCount++;

        return assetBundleItem.AssetBundle;
    }
}

/// <summary>
///  ��¼��ǰÿ��Bundle������ �������ж���ظ�
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
/// AB���ñ�����ԴItem ����ABBase
/// </summary>
public class ResourceItem
{
    public uint Crc; // ·����ӦCrc
    public string ABName; //  AB����
    public string AssetName; // ��Դ����
    public List<string> Dependce; // �����б�
    public AssetBundle AssetBundle; // ������ɵ�AssetBundle
}