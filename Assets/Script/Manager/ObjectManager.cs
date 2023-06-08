using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象管理器
/// </summary>
public class ObjectManager : Singleton<ObjectManager>
{
    #region 类对象池

    // 存储 类型 对应 类对象池 字典
    protected Dictionary<Type, object> m_TypePoolDic = new Dictionary<Type, object>();

    /// <summary>
    /// 创建该类的对象池 如果存在则直接获取
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxCount">创建时的最大存储个数</param>
    /// <returns></returns>
    public ClassObjectPool<T> GetOrCreateClassObjectPool<T>(int maxCount) where T : class, new()
    {
        Type type = typeof(T);

        // 类型对象池字典没有
        if (!m_TypePoolDic.TryGetValue(type, out object obj) || obj == null)
        {
            ClassObjectPool<T> classPool = new ClassObjectPool<T>(maxCount);
            m_TypePoolDic.Add(type, classPool);
            return classPool;
        }

        return obj as ClassObjectPool<T>;
    }

    #endregion 类对象池


}
