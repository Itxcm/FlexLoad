using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ResourceManager;

public class ResourceManager : Singleton<ResourceManager>
{
    // 是否从AssetBundle中加载
    public bool IsLoadFromAssetBundle = true;
    // 正在使用的资源字典 crc路径对应资源
    public Dictionary<uint, ResourceItem> _assetDic = new Dictionary<uint, ResourceItem>();
    // 未使用的资源Map(引用计数为0) 达到最大清除最早未使用的
    public DoubleLinkedMap<ResourceItem> resourceMap = new DoubleLinkedMap<ResourceItem>();

    #region 同步加载

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : Object
    {
        if (string.IsNullOrEmpty(path)) return null;

        uint crc = Crc32.GetCrc32(path);
        ResourceItem item = GetCacheResource(crc);

        // 资源存在 直接返回 资源不存在 加载资源并缓存后返回
        return item?.Object as T ?? LoadAssetAndCache<T>(path, crc);
    }

    /// <summary>
    /// 同步加载资源并缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <param name="crc"></param>
    /// <returns></returns>
    private T LoadAssetAndCache<T>(string path, uint crc) where T : Object
    {
        ResourceItem item;
        Object obj = null;

        // AB包中加载
        if (IsLoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.LoadResourceItem(crc);
            obj = null;

            if (item?.AssetBundle != null)
            {
                obj = item.Object as T ?? item.AssetBundle.LoadAsset<T>(item.AssetName);
            }
        }
        // 编辑器加载
        else
        {
            item = AssetBundleManager.Instance.GetResourceByCrcPath(crc);
            obj = item?.Object as T ?? LoadAssetByEditor<T>(path);
        }

        // 缓存资源
        return CacheResource<T>(crc, path, ref item, obj);
    }

    #endregion 同步加载

    #region 异步加载

    public delegate void OnAsyncLoadFinish(string path, Object obj, params object[] param); // 异步完成回调

    private MonoBehaviour mono;  // 开启携程的mono脚本
    private long lastYieldTime; // 上次加载完成时间
    private const long MAXLONGRESETIME = 200000; // 最大异步加载时间 单位 微秒

    private Dictionary<uint, AsyncTask> _asyncTaskDic = new Dictionary<uint, AsyncTask>();  // 正在异步加载的资源字典 存所有类型 路径为key
    private List<AsyncTask>[] _asyncTaskList = new List<AsyncTask>[(int)AsyncLoadPriority.RES_NUM]; // 正在异步加载的资源列表 存每个类型对应一个列表

    private ClassObjectPool<AsyncTask> _asyncTaskPool = ObjectManager.Instance.GetOrCreateClassObjectPool<AsyncTask>(50); // 异步加载参数对象池
    private ClassObjectPool<AsyncLoadCallBack> _asyncLoadCallBackPool = ObjectManager.Instance.GetOrCreateClassObjectPool<AsyncLoadCallBack>(100); // 异步加载完成对象池

    /// <summary>
    /// 异步加载初始化
    /// </summary>
    /// <param name="mono"></param>
    public void Init(MonoBehaviour mono)
    {
        for (int i = 0; i < (int)AsyncLoadPriority.RES_NUM; i++) _asyncTaskList[i] = new List<AsyncTask>();
        this.mono = mono;
        mono.StartCoroutine(LoadResourceCor());
    }

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="crc"></param>
    /// <param name="priority"></param>
    /// <param name="finishCall"></param>
    /// <param name="param"></param>
    public void LoadResourceAsync(string path, AsyncLoadPriority priority, bool isSprite, OnAsyncLoadFinish finishCall, params object[] param)
    {
        uint crc = Crc32.GetCrc32(path);

        // 资源缓存了直接加载触发 没缓存 添加异步任务 给异步任务添加完成回调

        ResourceItem item = GetCacheResource(crc);
        if (item != null)
        {
            finishCall?.Invoke(path, item.Object, param);
            return;
        }

        // 查找是否存在异步任务 不存在则添加 存在则拿出来添加回调
        AsyncTask asyncTask = GetOrAddAsyncTask(crc, priority, path, isSprite);

        // 指定异步任务添加完成回调
        AddAsyncCallBack(asyncTask, finishCall, param);
    }

    /// <summary>
    /// 异步加载资源携程器
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadResourceCor()
    {
        while (true)
        {
            bool haveYield = false;
            // 遍历异步加载列表 对每种优先级进行处理
            for (int i = 0; i < _asyncTaskList.Length; i++)
            {
                List<AsyncTask> asyncList = _asyncTaskList[i];

                if (asyncList.Count <= 0) continue;

                // 这里必须拿出的时候就移除 不能等加载完再移除
                AsyncTask task = asyncList[0];
                asyncList.RemoveAt(0);

                // 异步加载资源 缓存并执行回调
                mono.StartCoroutine(AsycnLoadAndCache(task));

                // 指定超过最大加载时间再继续
                if (System.DateTime.Now.Ticks - lastYieldTime > MAXLONGRESETIME)
                {
                    yield return null;
                    lastYieldTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            // 指定超过最大加载时间再继续
            if (!haveYield || System.DateTime.Now.Ticks - lastYieldTime > MAXLONGRESETIME)
            {
                lastYieldTime = System.DateTime.Now.Ticks;
                yield return null;
            }
        }

    }

    /// <summary>
    /// 异步加载资源 缓存并执行回调
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

        // AB包加载
        if (IsLoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.LoadResourceItem(asyncTask.Crc);

            if (item?.AssetBundle != null)
            {
                // 这里需要准确判断是否是Sprite
                AssetBundleRequest request = asyncTask.IsSprite ? item.AssetBundle.LoadAssetAsync<Sprite>(item.AssetName) : item.AssetBundle.LoadAssetAsync(item.AssetName);

                while (!request.isDone)
                {
                    yield return null;
                }

                obj = request.asset;
                lastYieldTime = System.DateTime.Now.Ticks;
            }
        }
        // 编辑器加载
        else
        {
            obj = LoadAssetByEditor<Object>(asyncTask.Path);

            yield return new WaitForSeconds(0.1f);   // 模拟异步加载

            item = AssetBundleManager.Instance.GetResourceByCrcPath(asyncTask.Crc);
        }

        // 缓存资源
        CacheResource<Object>(asyncTask.Crc, asyncTask.Path, ref item, obj, asyncTask.CallBackList.Count); // 这里的引用数量取决于回调数量

        // 执行加载完成回调
        RemoveAsyncCallBack(asyncTask, obj);

        // 移除异步任务
        RemoveAsyncTask(asyncTask);

    }

    #region 异步中间类以及枚举

    /// <summary>
    /// 异步任务
    /// </summary>
    public class AsyncTask
    {
        public List<AsyncLoadCallBack> CallBackList = new List<AsyncLoadCallBack>(); // 已经加载完成的回调列表
        public uint Crc; // Crc标识
        public string Path; // 资源路径
        public AsyncLoadPriority Priority; // 优先级
        public bool IsSprite; // 是否是图片 (由于异步加载图片需要指定加载类型)

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
    ///  异步加载完成
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
    /// 异步加载优先级
    /// </summary>
    public enum AsyncLoadPriority
    {
        RES_HIGHT = 0,
        RES_MIDDLE,
        RES_SLOW,
        RES_NUM
    }

    #endregion 异步中间类以及枚举

    #region 异步任务处理

    /// <summary>
    /// 添加异步任务
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

            _asyncTaskDic.Add(crc, asyncTask); // 添加到异步任务字典
            _asyncTaskList[(int)priority].Add(asyncTask); // 添加到异步任务列表
        }
        return asyncTask;
    }

    /// <summary>
    /// 移除异步任务
    /// </summary>
    /// <param name="asyncTask"></param>
    private void RemoveAsyncTask(AsyncTask asyncTask)
    {
        _asyncTaskDic.Remove(asyncTask.Crc);  //  从异步任务字典移除 

        asyncTask.Reset();
        _asyncTaskPool.Recyle(asyncTask);
    }

    /// <summary>
    /// 添加异步完成回调
    /// </summary>
    private void AddAsyncCallBack(AsyncTask asyncTask, OnAsyncLoadFinish finishCall, params object[] param)
    {
        AsyncLoadCallBack callBack = _asyncLoadCallBackPool.Spawn(true);
        callBack.FinishCall = finishCall;
        callBack.Params = param;
        asyncTask.CallBackList.Add(callBack);
    }

    /// <summary>
    /// 触发并移除异步完成回调
    /// </summary>
    /// <param name="asyncTask"></param>
    /// <param name="obj"></param>
    private void RemoveAsyncCallBack(AsyncTask asyncTask, Object obj)
    {
        // 执行加载完成回调
        for (int i = 0; i < asyncTask.CallBackList.Count; i++)
        {
            // 触发回调 并滞空
            AsyncLoadCallBack callBack = asyncTask.CallBackList[i];
            if (callBack?.FinishCall != null)
            {
                callBack.FinishCall.Invoke(asyncTask.Path, obj, callBack.Params);
                callBack.FinishCall = null;
            }

            // 清空回收
            callBack.Reset();
            _asyncLoadCallBackPool.Recyle(callBack);
        }
    }

    #endregion 异步任务处理

    #endregion 异步加载

    #region 资源卸载

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

    #endregion 资源卸载

    #region 资源操作

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
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
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

        // 添加到引用缓存的字典
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

    #endregion 资源操作

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

