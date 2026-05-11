using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace ToolKits
{
    public abstract class CustomPoolSingleton<T, T1> : Singleton<T1> where T1 : Singleton<T1> where T : Object
    {
        [SerializeField] protected T poolObj;
        protected ObjectPool<T> pool;
        protected List<T> currentObj = new List<T>();
        protected int maxPoolSize = 100;

        protected virtual void Awake()
        {
            pool = new ObjectPool<T>(Create, Get, Release, PoolDestroy, false, 0, maxPoolSize);
        }

        protected virtual T Create()
        {
            T obj = GameObject.Instantiate(poolObj, this.transform);
            return obj;
        }

        protected virtual void Get(T obj)
        {
            currentObj.Add(obj);
        }

        protected virtual void Release(T obj)
        {
            currentObj.Remove(obj);
        }

        protected virtual void PoolDestroy(T obj)
        {
            Destroy(obj);
        }

        public void ClearAll()
        {
            while (currentObj.Count > 0)
            {
                pool.Release(currentObj[0]);
            }

            currentObj.Clear();
        }
    }

    public abstract class CustomPool<T> : MonoBehaviour where T : Object
    {
        [SerializeField] protected T poolObj;
        protected ObjectPool<T> pool;
        protected List<T> currentObj = new List<T>();
        protected int maxPoolSize = 100;

        protected virtual void Awake()
        {
            pool = new ObjectPool<T>(Create, Get, Release, PoolDestroy, false, 0, maxPoolSize);
        }

        protected virtual T Create()
        {
            T obj = GameObject.Instantiate(poolObj, this.transform);

            return obj;
        }

        protected virtual void Get(T obj)
        {
            currentObj.Add(obj);
        }

        protected virtual void Release(T obj)
        {
            currentObj.Remove(obj);
        }

        protected virtual void PoolDestroy(T obj)
        {
            Destroy(obj);
        }

        public void ClearAll()
        {
            while (currentObj.Count > 0)
            {
                pool.Release(currentObj[0]);
            }

            currentObj.Clear();
        }
    }

    public abstract class CustomPool : MonoBehaviour
    {
        [SerializeField] protected GameObject poolObj;
        protected ObjectPool<GameObject> pool;
        protected List<GameObject> currentObj = new List<GameObject>();
        protected int maxPoolSize = 100;

        protected virtual void Awake()
        {
            pool = new ObjectPool<GameObject>(Create, Get, Release, PoolDestroy, false, 0, maxPoolSize);
        }

        protected virtual GameObject Create()
        {
            GameObject obj = GameObject.Instantiate(poolObj, this.transform);
            return obj;
        }

        protected virtual void Get(GameObject obj)
        {
            obj.gameObject.SetActive(true);
            currentObj.Add(obj);
        }

        protected virtual void Release(GameObject obj)
        {
            obj.gameObject.SetActive(false);
            currentObj.Remove(obj);
        }

        protected virtual void PoolDestroy(GameObject obj)
        {
            Destroy(obj);
        }

        public void ClearAll()
        {
            while (currentObj.Count > 0)
            {
                pool.Release(currentObj[0]);
            }

            currentObj.Clear();
        }
    }

    public abstract class CustomPoolSingleton<T> : Singleton<T> where T : Singleton<T>
    {
        [SerializeField] protected GameObject poolObj;

        protected ObjectPool<GameObject> pool;
        protected List<GameObject> currentObj = new List<GameObject>();
        protected int maxPoolSize = 100;

        protected virtual void Awake()
        {
            pool = new ObjectPool<GameObject>(Create, Get, Release, PoolDestroy, false, 0, maxPoolSize);
        }

        protected virtual GameObject Create()
        {
            GameObject obj = GameObject.Instantiate(poolObj, this.transform);

            return obj;
        }

        protected virtual void Get(GameObject obj)
        {
            obj.gameObject.SetActive(true);
            currentObj.Add(obj);
        }

        protected virtual void Release(GameObject obj)
        {
            obj.gameObject.SetActive(false);
            currentObj.Remove(obj);
        }

        protected virtual void PoolDestroy(GameObject obj)
        {
            Destroy(obj);
        }

        public void ClearAll()
        {
            while (currentObj.Count > 0)
            {
                pool.Release(currentObj[0]);
            }

            currentObj.Clear();
        }
    }

    public class GameobjectPool
    {
        private GameObject poolObj;
        private ObjectPool<GameObject> pool;
        private Transform createRoot;
        public HashSet<GameObject> activeObjs;

        public GameobjectPool(GameObject prefab, Transform parent, int defaultCapacity = 10, int maxPoolSize = 100,
            Func<GameObject> onCreate = null, Action<GameObject> onGet = null, Action<GameObject> onRelease = null)
        {
            activeObjs = new HashSet<GameObject>();
            poolObj = prefab;
            createRoot = parent;
            pool = new ObjectPool<GameObject>(
                onCreate ?? ActionOnCreate,
                onGet ?? ActionOnGet,
                onRelease ?? ActionOnRelease,
                ActionOnDestroy,
                false,
                defaultCapacity,
                maxPoolSize
            );
        }

     
        private GameObject ActionOnCreate()
        {
            GameObject obj = GameObject.Instantiate(poolObj, createRoot);
            return obj;
        }


        private void ActionOnGet(GameObject obj)
        {
            obj.SetActive(true);
            activeObjs.Add(obj);
        }

        private void ActionOnRelease(GameObject obj)
        {
            obj.SetActive(false);
            activeObjs.Remove(obj);
        }

        private void ActionOnDestroy(GameObject obj)
        {
            GameObject.Destroy(obj);
        }

        public GameObject Get()
        {
            return pool.Get();
        }

        public void Release(GameObject obj)
        {
            pool.Release(obj);
        }


        public void Clear()
        {
            GameObject[] buffer = new GameObject[activeObjs.Count];
            activeObjs.CopyTo(buffer, 0);

            for (int i = 0; i < buffer.Length; i++)
                pool.Release(buffer[i]);

            activeObjs.Clear();
        }
    }
}