using System.Collections.Generic;

/// <summary>
///  类对象池
/// </summary>
/// <typeparam name="T"></typeparam>
public class ClassObjectPool<T> where T : class, new()
{
    protected Stack<T> pool = new Stack<T>(); // 池
    protected int maxCount; // 最大对象个数
    protected int noRecyleCount; // 没有回收的个数 当前存在的个数

    public ClassObjectPool(int maxCount)
    {
        this.maxCount = maxCount;
        for (int i = 0; i < maxCount; i++) pool.Push(new T());
    }

    /// <summary>
    /// 从对象池中取一个对象
    /// </summary>
    /// <param name="createIfEmpty">为空是否new出来</param>
    /// <returns></returns>
    public T Spawn(bool createIfEmpty)
    {
        T obj;
        // 池中有
        if (pool.Count > 0)
        {
            obj = pool.Pop();
            if (obj == null && createIfEmpty) obj = new T();
            noRecyleCount++;
            return obj;
        }
        // 池中没有 为空需要创建
        else if (createIfEmpty)
        {
            obj = new T();
            noRecyleCount++;
            return obj;
        }
        // 池中没有 为空不创建
        return null;
    }

    /// <summary>
    /// 将对象回收进入对象池
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Recyle(T obj)
    {
        if (obj == null) return false;

        // 池中存了最大数量
        if (pool.Count >= maxCount && maxCount > 0) return false;

        pool.Push(obj);
        noRecyleCount--;

        return true;
    }

    /// <summary>
    /// 获取未回收对象个数
    /// </summary>
    /// <returns></returns>
    public int GetNoCycleCount() => noRecyleCount;
}