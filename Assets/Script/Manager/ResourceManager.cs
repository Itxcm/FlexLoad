using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ResourceManager;

public class ResourceManager : Singleton<ResourceManager>
{
    // �Ƿ��AssetBundle�м���
    public bool IsLoadFromAssetBundle = true;
    // ����ʹ�õ���Դ�ֵ� crc·����Ӧ��Դ
    public Dictionary<uint, ResourceItem> _assetDic = new Dictionary<uint, ResourceItem>();
    // δʹ�õ���ԴMap(���ü���Ϊ0) �ﵽ����������δʹ�õ�
    public DoubleLinkedMap<ResourceItem> resourceMap = new DoubleLinkedMap<ResourceItem>();

    #region ͬ������

    /// <summary>
    /// ͬ������
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return null;

        uint crc = Crc32.GetCrc32(path);
        ResourceItem item = GetCacheResource(crc);

        // ��Դ���� ֱ�ӷ��� ��Դ������ ������Դ������󷵻�
        return item?.Object as T ?? LoadAssetAndCache<T>(path, crc);
    }

    /// <summary>
    /// ͬ��������Դ������
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="crc"></param>
    /// <returns></returns>
    private T LoadAssetAndCache<T>(string path, uint crc) where T : Object
    {
        ResourceItem item;
        Object obj = null;

        // AB���м���
        if (IsLoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.LoadResourceItem(crc);
            obj = null;

            if (item?.AssetBundle != null)
            {
                obj = item.Object as T ?? item.AssetBundle.LoadAsset<T>(item.AssetName);
            }
        }
        // �༭������
        else
        {
            item = AssetBundleManager.Instance.GetResourceByCrcPath(crc);
            obj = item?.Object as T ?? LoadAssetByEditor<T>(path);
        }

        // ������Դ
        return CacheResource<T>(crc, path, ref item, obj);
    }

    #endregion ͬ������

    #region �첽����

    public delegate void OnAsyncLoadFinish(string path, Object obj, params object[] param); // �첽��ɻص�

    private MonoBehaviour mono;  // ����Я�̵�mono�ű�
    private long lastYieldTime; // �ϴμ������ʱ��
    private const long MAXLONGRESETIME = 200000; // ����첽����ʱ�� ��λ ΢��

    private Dictionary<uint, AsyncTask> _asyncTaskDic = new Dictionary<uint, AsyncTask>();  // �����첽���ص���Դ�ֵ� ���������� ·��Ϊkey
    private List<AsyncTask>[] _asyncTaskList = new List<AsyncTask>[(int)AsyncLoadPriority.RES_NUM]; // �����첽���ص���Դ�б� ��ÿ�����Ͷ�Ӧһ���б�

    private ClassObjectPool<AsyncTask> _asyncTaskPool = ObjectManager.Instance.GetOrCreateClassObjectPool<AsyncTask>(50); // �첽���ز��������
    private ClassObjectPool<AsyncLoadCallBack> _asyncLoadCallBackPool = ObjectManager.Instance.GetOrCreateClassObjectPool<AsyncLoadCallBack>(100); // �첽������ɶ����

    /// <summary>
    /// �첽���س�ʼ��
    /// </summary>
    /// <param name="mono"></param>
    public void Init(MonoBehaviour mono)
    {
        for (int i = 0; i < (int)AsyncLoadPriority.RES_NUM; i++) _asyncTaskList[i] = new List<AsyncTask>();
        this.mono = mono;
        mono.StartCoroutine(LoadResourceCor());
    }

    /// <summary>
    /// �첽������Դ
    /// </summary>
    /// <param name="path"></param>
    /// <param name="crc"></param>
    /// <param name="priority"></param>
    /// <param name="finishCall"></param>
    /// <param name="param"></param>
    public void LoadResourceAsync(string path, AsyncLoadPriority priority, bool isSprite, OnAsyncLoadFinish finishCall, params object[] param)
    {
        uint crc = Crc32.GetCrc32(path);

        // ��Դ������ֱ�Ӽ��ش��� û���� ����첽���� ���첽���������ɻص�

        ResourceItem item = GetCacheResource(crc);
        if (item != null)
        {
            finishCall?.Invoke(path, item.Object, param);
            return;
        }

        // �����Ƿ�����첽���� ����������� �������ó�����ӻص�
        AsyncTask asyncTask = GetOrAddAsyncTask(crc, priority, path, isSprite);

        // ָ���첽���������ɻص�
        AddAsyncCallBack(asyncTask, finishCall, param);
    }

    /// <summary>
    /// �첽������ԴЯ����
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadResourceCor()
    {
        while (true)
        {
            bool haveYield = false;
            // �����첽�����б� ��ÿ�����ȼ����д���
            for (int i = 0; i < _asyncTaskList.Length; i++)
            {
                List<AsyncTask> asyncList = _asyncTaskList[i];

                if (asyncList.Count <= 0) continue;

                // ��������ó���ʱ����Ƴ� ���ܵȼ��������Ƴ�
                AsyncTask task = asyncList[0];
                asyncList.RemoveAt(0);

                // �첽������Դ ���沢ִ�лص�
                mono.StartCoroutine(AsycnLoadAndCache(task));

                // ָ������������ʱ���ټ���
                if (System.DateTime.Now.Ticks - lastYieldTime > MAXLONGRESETIME)
                {
                    yield return null;
                    lastYieldTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            // ָ������������ʱ���ټ���
            if (!haveYield || System.DateTime.Now.Ticks - lastYieldTime > MAXLONGRESETIME)
            {
                lastYieldTime = System.DateTime.Now.Ticks;
                yield return null;
            }
        }

    }

    /// <summary>
    /// �첽������Դ ���沢ִ�лص�
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="crc"></param>
    /// <param name="asyncTask"></param>
    /// <returns></returns>
    private IEnumerator AsycnLoadAndCache(AsyncTask asyncTask)
    {
        Object obj = null;
        ResourceItem item = null;

        // AB������
        if (IsLoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.LoadResourceItem(asyncTask.Crc);

            if (item?.AssetBundle != null)
            {
                // ������Ҫ׼ȷ�ж��Ƿ���Sprite
                AssetBundleRequest request = asyncTask.IsSprite ? item.AssetBundle.LoadAssetAsync<Sprite>(item.AssetName) : item.AssetBundle.LoadAssetAsync(item.AssetName);

                while (!request.isDone)
                {
                    yield return null;
                }

                obj = request.asset;
                lastYieldTime = System.DateTime.Now.Ticks;
            }
        }
        // �༭������
        else
        {
            obj = LoadAssetByEditor<Object>(asyncTask.Path);

            yield return new WaitForSeconds(0.1f);   // ģ���첽����

            item = AssetBundleManager.Instance.GetResourceByCrcPath(asyncTask.Crc);
        }

        // ������Դ
        CacheResource<Object>(asyncTask.Crc, asyncTask.Path, ref item, obj, asyncTask.CallBackList.Count); // �������������ȡ���ڻص�����

        // ִ�м�����ɻص�
        RemoveAsyncCallBack(asyncTask, obj);

        // �Ƴ��첽����
        RemoveAsyncTask(asyncTask);

    }

    #region �첽�м����Լ�ö��

    /// <summary>
    /// �첽����
    /// </summary>
    public class AsyncTask
    {
        public List<AsyncLoadCallBack> CallBackList = new List<AsyncLoadCallBack>(); // �Ѿ�������ɵĻص��б�
        public uint Crc; // Crc��ʶ
        public string Path; // ��Դ·��
        public AsyncLoadPriority Priority; // ���ȼ�
        public bool IsSprite; // �Ƿ���ͼƬ (�����첽����ͼƬ��Ҫָ����������)

        public void Reset()
        {
            Crc = 0;
            Path = "";
            IsSprite = false;
            Priority = AsyncLoadPriority.RES_SLOW;
            CallBackList.Clear();
        }
    }
    /// <summary>
    ///  �첽�������
    /// </summary>
    public class AsyncLoadCallBack
    {
        public OnAsyncLoadFinish FinishCall;
        public object[] Params;

        public void Reset()
        {
            FinishCall = null;
            Params = null;
        }
    }
    /// <summary>
    /// �첽�������ȼ�
    /// </summary>
    public enum AsyncLoadPriority
    {
        RES_HIGHT = 0,
        RES_MIDDLE,
        RES_SLOW,
        RES_NUM
    }

    #endregion �첽�м����Լ�ö��

    #region �첽������

    /// <summary>
    /// ����첽����
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="priority"></param>
    /// <param name="path"></param>
    /// <param name="isSprite"></param>
    private AsyncTask GetOrAddAsyncTask(uint crc, AsyncLoadPriority priority, string path, bool isSprite)
    {
        if (!_asyncTaskDic.TryGetValue(crc, out AsyncTask asyncTask) || asyncTask == null)
        {
            asyncTask = _asyncTaskPool.Spawn(true);
            asyncTask.Crc = crc;
            asyncTask.Priority = priority;
            asyncTask.Path = path;
            asyncTask.IsSprite = isSprite;

            _asyncTaskDic.Add(crc, asyncTask); // ��ӵ��첽�����ֵ�
            _asyncTaskList[(int)priority].Add(asyncTask); // ��ӵ��첽�����б�
        }
        return asyncTask;
    }

    /// <summary>
    /// �Ƴ��첽����
    /// </summary>
    /// <param name="asyncTask"></param>
    private void RemoveAsyncTask(AsyncTask asyncTask)
    {
        _asyncTaskDic.Remove(asyncTask.Crc);  //  ���첽�����ֵ��Ƴ� 

        asyncTask.Reset();
        _asyncTaskPool.Recyle(asyncTask);
    }

    /// <summary>
    /// ����첽��ɻص�
    /// </summary>
    private void AddAsyncCallBack(AsyncTask asyncTask, OnAsyncLoadFinish finishCall, params object[] param)
    {
        AsyncLoadCallBack callBack = _asyncLoadCallBackPool.Spawn(true);
        callBack.FinishCall = finishCall;
        callBack.Params = param;
        asyncTask.CallBackList.Add(callBack);
    }

    /// <summary>
    /// �������Ƴ��첽��ɻص�
    /// </summary>
    /// <param name="asyncTask"></param>
    /// <param name="obj"></param>
    private void RemoveAsyncCallBack(AsyncTask asyncTask, Object obj)
    {
        // ִ�м�����ɻص�
        for (int i = 0; i < asyncTask.CallBackList.Count; i++)
        {
            // �����ص� ���Ϳ�
            AsyncLoadCallBack callBack = asyncTask.CallBackList[i];
            if (callBack?.FinishCall != null)
            {
                callBack.FinishCall.Invoke(asyncTask.Path, obj, callBack.Params);
                callBack.FinishCall = null;
            }

            // ��ջ���
            callBack.Reset();
            _asyncLoadCallBackPool.Recyle(callBack);
        }
    }

    #endregion �첽������

    #endregion �첽����

    #region ��Դж��

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

    #endregion ��Դж��

    #region ��Դ����

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
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
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
    private T CacheResource<T>(uint crc, string path, ref ResourceItem item, Object obj, int addRefCount = 1) where T : Object
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
        return obj as T;
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

    #endregion ��Դ����

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

