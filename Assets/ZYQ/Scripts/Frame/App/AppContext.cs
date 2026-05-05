using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    // 服务定位器，避免到处 FindObjectOfType 或强依赖单例
    public class AppContext
    {
        private readonly Dictionary<Type, ManagerBase> _managers = new();
        private readonly List<ManagerBase> _managerList = new();

        public void Register<T>(T manager) where T : ManagerBase
        {
            var type = typeof(T);

            if (_managers.ContainsKey(type))
                throw new InvalidOperationException($"[AppContext] {type.Name} 已注册");

            _managers[type] = manager;
            _managerList.Add(manager);
        }

        public T Get<T>() where T : ManagerBase
        {
            if (_managers.TryGetValue(typeof(T), out var mgr)) return (T)mgr;
            throw new InvalidOperationException($"[AppContext] {typeof(T).Name} 未注册");
        }

        public bool TryGet<T>(out T manager) where T : ManagerBase
        {
            var found = _managers.TryGetValue(typeof(T), out var mgr);
            manager = found ? (T)mgr : null;
            return found;
        }

        public void InitializeAll()
        {
            foreach (var manager in _managerList)
                manager.Init(this);
        }

        public void TickAll(float dt)
        {
            foreach (var manager in _managerList)
            {
                if (manager.IsInitialized)
                    manager.Tick(dt);
            }
        }

        public void DisposeAll()
        {
            for (int i = _managerList.Count - 1; i >= 0; i--)
                _managerList[i].Dispose();
        }

        public void Reinitialize<T>() where T : ManagerBase
        {
            Get<T>().Reinitialize(this);
        }
    }
}
