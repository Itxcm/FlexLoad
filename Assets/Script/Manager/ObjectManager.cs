using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���������
/// </summary>
public class ObjectManager : Singleton<ObjectManager>
{
    #region ������

    // �洢 ���� ��Ӧ ������ �ֵ�
    protected Dictionary<Type, object> m_TypePoolDic = new Dictionary<Type, object>();

    /// <summary>
    /// ��������Ķ���� ���������ֱ�ӻ�ȡ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxCount">����ʱ�����洢����</param>
    /// <returns></returns>
    public ClassObjectPool<T> GetOrCreateClassObjectPool<T>(int maxCount) where T : class, new()
    {
        Type type = typeof(T);

        // ���Ͷ�����ֵ�û��
        if (!m_TypePoolDic.TryGetValue(type, out object obj) || obj == null)
        {
            ClassObjectPool<T> classPool = new ClassObjectPool<T>(maxCount);
            m_TypePoolDic.Add(type, classPool);
            return classPool;
        }

        return obj as ClassObjectPool<T>;
    }

    #endregion ������


}
