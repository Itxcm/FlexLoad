using System.Collections.Generic;

/// <summary>
///  ������
/// </summary>
/// <typeparam name="T"></typeparam>
public class ClassObjectPool<T> where T : class, new()
{
    protected Stack<T> pool = new Stack<T>(); // ��
    protected int maxCount; // ���������
    protected int noRecyleCount; // û�л��յĸ��� ��ǰ���ڵĸ���

    public ClassObjectPool(int maxCount)
    {
        this.maxCount = maxCount;
        for (int i = 0; i < maxCount; i++) pool.Push(new T());
    }

    /// <summary>
    /// �Ӷ������ȡһ������
    /// </summary>
    /// <param name="createIfEmpty">Ϊ���Ƿ�new����</param>
    /// <returns></returns>
    public T Spawn(bool createIfEmpty)
    {
        T obj;
        // ������
        if (pool.Count > 0)
        {
            obj = pool.Pop();
            if (obj == null && createIfEmpty) obj = new T();
            noRecyleCount++;
            return obj;
        }
        // ����û�� Ϊ����Ҫ����
        else if (createIfEmpty)
        {
            obj = new T();
            noRecyleCount++;
            return obj;
        }
        // ����û�� Ϊ�ղ�����
        return null;
    }

    /// <summary>
    /// ��������ս�������
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public bool Recyle(T obj)
    {
        if (obj == null) return false;

        // ���д����������
        if (pool.Count >= maxCount && maxCount > 0) return false;

        pool.Push(obj);
        noRecyleCount--;

        return true;
    }

    /// <summary>
    /// ��ȡδ���ն������
    /// </summary>
    /// <returns></returns>
    public int GetNoCycleCount() => noRecyleCount;
}