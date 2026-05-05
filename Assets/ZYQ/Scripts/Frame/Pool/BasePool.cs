using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System.Linq; // 用于 ToList

namespace ZYQ.Demo
{
    public class BasePool : MonoBehaviour
    {
        [SerializeField] protected GameObject prefab;
        [SerializeField] int defaultSize = 100;
        [SerializeField] int maxSize = 5000;

        // 修改这里：使用具体类而不是接口
        public ObjectPool<GameObject> pool;

        private HashSet<GameObject> activeObjects;

        // 现在这三个属性都可以正常访问了
        public int ActiveCount => pool.CountActive;     // 正在外面使用的数量
        public int InactiveCount => pool.CountInactive; // 留在池子里的数量
        public int TotalCount => pool.CountAll;         // 总生成的数量

        protected virtual void Awake()
        {
            activeObjects = new HashSet<GameObject>();

            // 显式实例化具体类
            pool = new ObjectPool<GameObject>(
                OnCreatePoolItem,
                OnGetPoolItem,
                OnReleasePoolItem,
                OnDestroyPoolItem,
                true,
                defaultSize,
                maxSize);
        }

        protected virtual GameObject OnCreatePoolItem()
        {
            if (prefab == null) return new GameObject("EmptyPoolItem");
            return Instantiate(prefab, transform);
        }

        protected virtual void OnGetPoolItem(GameObject obj)
        {
            obj.SetActive(true);
        }

        protected virtual void OnReleasePoolItem(GameObject obj)
        {
            obj.SetActive(false);
        }

        protected virtual void OnDestroyPoolItem(GameObject obj)
        {
            // 如果物体在池子销毁前被外部 Destroy 了，这里要判空
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        public GameObject Get()
        {
            GameObject o = pool.Get();

            // 安全处理：如果物体被外部意外销毁了
            if (o == null)
            {
                Debug.LogWarning($"池中获取到了已销毁的物体: {prefab.name}，正在创建新实例");
                return OnCreatePoolItem();
            }

            activeObjects.Add(o);
            return o;
        }

        public void Release(GameObject obj)
        {
            if (obj == null) return;

            // 检查物体是否属于本池子
            if (!activeObjects.Contains(obj))
            {
                Debug.LogWarning($"试图释放一个不属于此池子的物体: {obj.name}");
                return;
            }

            activeObjects.Remove(obj);
            pool.Release(obj);
        }

        public void ReleaseAll()
        {
            if (activeObjects.Count == 0) return;

            // 关键：转为 List 避免遍历时修改集合的异常
            List<GameObject> toRelease = activeObjects.ToList();

            foreach (GameObject go in toRelease)
            {
                if (go != null)
                {
                    pool.Release(go); // 注意这里不要调自己的 Release(go)，否则会重复 Remove
                }
            }

            activeObjects.Clear();
            Debug.Log($"已释放池 {prefab.name} 中的所有物体");
        }

        public void Clear() => pool.Clear();

        public void SetPrefab(GameObject newPrefab)
        {
            if (ActiveCount > 0)
            {
                Debug.LogWarning("池中尚有活跃物体时更换 Prefab 可能导致逻辑混乱");
            }
            this.prefab = newPrefab;
        }
    }
}