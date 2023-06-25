using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ResourceManager : Singleton<ResourceManager>
{
    // �Ƿ��AssetBundle�м���
    public bool IsLoadFromAssetBundle = false;

    // ����ʹ�õ���Դ�ֵ� ·����Ӧ��Դ
    public Dictionary<uint, ResourceItem> pathResourceDic = new Dictionary<uint, ResourceItem>();

    // δʹ�õ���ԴMap(���ü���Ϊ0) �ﵽ����������δʹ�õ�
    public DoubleLinkedMap<ResourceItem> resourceMap = new DoubleLinkedMap<ResourceItem>();

    // ����ָ����Դ
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
            obj = LoadAssetByEditor<T>(path);
            item = AssetBundleManager.Instance.GetResourceByCrcPath(crc);
        }
#endif

        // �Ǳ༭������ ��AB���м���

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

    // ����Crc·����ȡָ��������Դ
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

/// <summary>
/// ˫������ڵ�
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
///  ˫����
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
    /// ���һ���ڵ㵽ͷ��
    /// </summary>
    /// <param name="t">����</param>
    public DoubleLinkedListNode<T> AddToHead(T cul)
    {
        DoubleLinkedListNode<T> node = ContentPool.Spawn(true);
        node.Next = null;
        node.Prev = null;
        node.Cul = cul;
        return AddToHead(node);
    }

    /// <summary>
    /// ���һ���ڵ㵽ͷ��
    /// </summary>
    /// <param name="node">�ڵ�</param>
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
    /// ���һ���ڵ㵽β��
    /// </summary>
    /// <param name="t">����</param>
    public DoubleLinkedListNode<T> AddToTail(T cul)
    {
        DoubleLinkedListNode<T> node = ContentPool.Spawn(true);
        node.Next = null;
        node.Prev = null;
        node.Cul = cul;
        return AddToTail(node);
    }

    /// <summary>
    /// ���һ���ڵ㵽β��
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
    /// �Ƴ�ĳ���ڵ�
    /// </summary>
    /// <param name="node"></param>
    public void RemoveNode(DoubleLinkedListNode<T> node)
    {
        if (node == null) return;

        // ����ͷ��β
        if (node == Head)
            Head = Head.Next;
        if (node == Tail)
            Tail = Tail.Prev;

        // �����м�
        if (node.Prev != null)
            node.Prev.Next = node.Next;
        if (node.Next != null)
            node.Next.Prev = node.Prev;

        node.Reset();
        ContentPool.Recyle(node);
        _count--;
    }

    /// <summary>
    /// �ƶ��ڵ㵽ͷ��
    /// </summary>
    /// <param name="node"></param>
    public void MoveToHead(DoubleLinkedListNode<T> node)
    {
        if (node == null || node == Head) return;

        if (node.Prev == null && node.Next == null) return;

        // ����β��
        if (node == Tail)
            Tail = Tail.Prev;

        // �����м�
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
/// ˫�������װ
/// </summary>
/// <typeparam name="T"></typeparam>
public class DoubleLinkedMap<T> where T : class, new()
{
    // ˫����
    protected DoubleLinkedList<T> _list = new DoubleLinkedList<T>();

    // �ڵ��ֵ�  ���� : �ڵ�
    protected Dictionary<T, DoubleLinkedListNode<T>> _typeListDic = new Dictionary<T, DoubleLinkedListNode<T>>();

    /// <summary>
    /// �������� ��������б�
    /// </summary>
    ~DoubleLinkedMap()
    {
        Clear();
    }

    /// <summary>
    /// ���뵽ͷ��
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
    /// ��β������
    /// </summary>
    public T Pop()
    {
        T temp = _list.Tail.Cul;
        if (_list.Tail != null)
            Remove(_list.Tail.Cul);
        return temp;
    }

    /// <summary>
    /// �Ƴ�һ���ڵ�
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
    ///  ��ȡβ���ڵ�
    /// </summary>
    /// <returns></returns>
    public T Tail() => _list.Tail?.Cul;

    /// <summary>
    /// ���ؽڵ����
    /// </summary>
    /// <returns></returns>
    public int Size() => _typeListDic.Count;

    /// <summary>
    ///  ��ѯ�Ƿ���ڽڵ�
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
    ///  �ƶ�ĳ���ڵ㵽ͷ��
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
    /// ����б�
    /// </summary>
    public void Clear()
    {
        // һֱ�Ƴ�����
        while (_list.Tail != null)
        {
            Remove(_list.Tail.Cul);
        }
    }
}