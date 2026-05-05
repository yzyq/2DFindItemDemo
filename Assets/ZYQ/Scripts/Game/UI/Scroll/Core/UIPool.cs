using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    public sealed class UIPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Queue<T> pool = new Queue<T>();

        public UIPool(T prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public T Get()
        {
            T item;

            if (pool.Count > 0)
            {
                item = pool.Dequeue();
                item.gameObject.SetActive(true);
            }
            else
            {
                item = Object.Instantiate(prefab, parent);
            }

            item.transform.SetParent(parent, false);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;

            item.gameObject.SetActive(false);
            item.transform.SetParent(parent, false);
            pool.Enqueue(item);
        }

        public void Clear()
        {
            while (pool.Count > 0)
            {
                T item = pool.Dequeue();

                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }
        }
    }
}