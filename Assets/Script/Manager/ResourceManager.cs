using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    // 是否从AssetBundle中加载
    public bool IsLoadFromAssetBundle = false;

    // 正在使用的资源字典 路径对应资源
    public Dictionary<uint, ResourceItem> pathResourceDic = new Dictionary<uint, ResourceItem>();

    // 未使用的资源Map(引用计数为0) 达到最大清除最早未使用的
    public DoubleLinkedMap<ResourceItem> resourceMap = new DoubleLinkedMap<ResourceItem>();

    // 加载指定资源
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
            obj = LoadAssetByEditor<T>(path);
            item = AssetBundleManager.Instance.GetResourceByCrcPath(crc);
        }
#endif

        // 非编辑器环境 从AB包中加载

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceItem(crc);
            if (item != null && item.AssetBundle != null)
            {
                obj = item.AssetBundle.LoadAsset<T>(item.AssetName);
            }
        }

        return obj;
    }

    // 根据Crc路径获取指定缓存资源
    public ResourceItem GetCacheResource(uint crc, int refCount = 1)
    {
        if (pathResourceDic.TryGetValue(crc, out ResourceItem item) && item != null)
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

/// <summary>
/// 双向链表节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLinkedListNode<T> where T : class, new()
{
    public DoubleLinkedListNode<T> Prev;
    public DoubleLinkedListNode<T> Next;
    public T Cul;

    public void Reset()
    {
        Prev = null;
        Next = null;
        Cul = null;
    }
}

/// <summary>
///  双链表
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLinkedList<T> where T : class, new()
{
    public DoubleLinkedListNode<T> Head;
    public DoubleLinkedListNode<T> Tail;
    protected ClassObjectPool<DoubleLinkedListNode<T>> ContentPool = ObjectManager.Instance.GetOrCreateClassObjectPool<DoubleLinkedListNode<T>>(500);
    protected int _count;
    public int Count => _count;

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="t">类型</param>
    public DoubleLinkedListNode<T> AddToHead(T cul)
    {
        DoubleLinkedListNode<T> node = ContentPool.Spawn(true);
        node.Next = null;
        node.Prev = null;
        node.Cul = cul;
        return AddToHead(node);
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="node">节点</param>
    public DoubleLinkedListNode<T> AddToHead(DoubleLinkedListNode<T> node)
    {
        if (node == null) return null;

        node.Prev = null;

        if (Head == null)
        {
            Head = Tail = node;
        }
        else
        {
            Head.Prev = node;
            node.Next = Head;
            Head = node;
        }
        _count++;
        return Head;
    }

    /// <summary>
    /// 添加一个节点到尾部
    /// </summary>
    /// <param name="t">类型</param>
    public DoubleLinkedListNode<T> AddToTail(T cul)
    {
        DoubleLinkedListNode<T> node = ContentPool.Spawn(true);
        node.Next = null;
        node.Prev = null;
        node.Cul = cul;
        return AddToTail(node);
    }

    /// <summary>
    /// 添加一个节点到尾部
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public DoubleLinkedListNode<T> AddToTail(DoubleLinkedListNode<T> node)
    {
        if (node == null) return null;

        node.Next = null;

        if (Tail == null)
        {
            Tail = Head = node;
        }
        else
        {
            Tail.Next = node;
            node.Prev = Tail;
            Tail = node;
        }
        _count++;
        return Tail;
    }

    /// <summary>
    /// 移除某个节点
    /// </summary>
    /// <param name="node"></param>
    public void RemoveNode(DoubleLinkedListNode<T> node)
    {
        if (node == null) return;

        // 处理头和尾
        if (node == Head)
            Head = Head.Next;
        if (node == Tail)
            Tail = Tail.Prev;

        // 处理中间
        if (node.Prev != null)
            node.Prev.Next = node.Next;
        if (node.Next != null)
            node.Next.Prev = node.Prev;

        node.Reset();
        ContentPool.Recyle(node);
        _count--;
    }

    /// <summary>
    /// 移动节点到头部
    /// </summary>
    /// <param name="node"></param>
    public void MoveToHead(DoubleLinkedListNode<T> node)
    {
        if (node == null || node == Head) return;

        if (node.Prev == null && node.Next == null) return;

        // 处理尾部
        if (node == Tail)
            Tail = Tail.Prev;

        // 处理中间
        if (node.Prev != null)
            node.Prev.Next = node.Next;
        if (node.Next != null)
            node.Next.Prev = node.Prev;

        node.Prev = null;
        node.Next = Head;
        Head.Prev = node;
        Head = node;

        Tail ??= Head;
    }
}

/// <summary>
/// 双向链表封装
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLinkedMap<T> where T : class, new()
{
    // 双链表
    protected DoubleLinkedList<T> _list = new DoubleLinkedList<T>();

    // 节点字典  类型 : 节点
    protected Dictionary<T, DoubleLinkedListNode<T>> _typeListDic = new Dictionary<T, DoubleLinkedListNode<T>>();

    /// <summary>
    /// 析构函数 主动情况列表
    /// </summary>
    ~DoubleLinkedMap()
    {
        Clear();
    }

    /// <summary>
    /// 插入到头部
    /// </summary>
    /// <param name="t"></param>
    public void Insert(T t)
    {
        if (_typeListDic.TryGetValue(t, out DoubleLinkedListNode<T> node) && node != null)
        {
            _list.AddToHead(node);
            return;
        }
        _list.AddToHead(t);
        _typeListDic.Add(t, _list.Head);
    }

    /// <summary>
    /// 从尾部弹出
    /// </summary>
    public T Pop()
    {
        T temp = _list.Tail.Cul;
        if (_list.Tail != null)
            Remove(_list.Tail.Cul);
        return temp;
    }

    /// <summary>
    /// 移除一个节点
    /// </summary>
    /// <param name="t"></param>
    public void Remove(T t)
    {
        if (!_typeListDic.TryGetValue(t, out DoubleLinkedListNode<T> node) || node == null)
        {
            return;
        }
        _list.RemoveNode(node);
        _typeListDic.Remove(t);
    }

    /// <summary>
    ///  获取尾部节点
    /// </summary>
    /// <returns></returns>
    public T Tail() => _list.Tail?.Cul;

    /// <summary>
    /// 返回节点个数
    /// </summary>
    /// <returns></returns>
    public int Size() => _typeListDic.Count;

    /// <summary>
    ///  查询是否存在节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t)
    {
        if (!_typeListDic.TryGetValue(t, out DoubleLinkedListNode<T> node) || node == null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    ///  移动某个节点到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Move(T t)
    {
        if (!_typeListDic.TryGetValue(t, out DoubleLinkedListNode<T> node) || node == null)
        {
            return false;
        }
        _list.MoveToHead(node);

        return true;
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    public void Clear()
    {
        // 一直移除到空
        while (_list.Tail != null)
        {
            Remove(_list.Tail.Cul);
        }
    }
}