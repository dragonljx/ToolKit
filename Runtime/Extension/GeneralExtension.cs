using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using UnityEngine;
/// <summary>
/// 通用扩展方法
/// </summary>
public static class GeneralExtension 
{
    /// <summary>
    /// 在不重复的情况下添加对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dic"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool AddWithout<T>(this Collection<T> dic, T obj)
    {
        if (!dic.Contains(obj))
        {
            dic.Add(obj);
            return true;
        }
        return false;
    }

}
