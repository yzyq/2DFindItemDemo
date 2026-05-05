using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    public class UIPanelPool
    {
        private Dictionary<UIPanelType, Queue<UIPanel>> pool = new();

        public T Get<T>(UIPanelType type) where T : UIPanel
        {
            if (pool.TryGetValue(type, out var q) && q.Count > 0)
                return (T)q.Dequeue();

            return null;
        }

        public void Recycle(UIPanel panel)
        {
            var type = panel.Type;

            if (!pool.ContainsKey(type))
                pool[type] = new Queue<UIPanel>();

            panel.gameObject.SetActive(false);
            pool[type].Enqueue(panel);
        }
    }
}
