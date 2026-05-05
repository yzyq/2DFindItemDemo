using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    public class AppFacade
    {
        private static readonly Dictionary<Type, object> managers = new();

        private static GameObject root;

        #region Init

        public static void Init()
        {
            if (root == null)
            {
                root = new GameObject("[AppRoot]");
                GameObject.DontDestroyOnLoad(root);
            }
        }

        #endregion

        #region Register

        public static T RegisterManager<T>() where T : Component
        {
            Type type = typeof(T);

            if (managers.TryGetValue(type, out var existing))
                return (T)existing;

            var comp = root.AddComponent<T>();

            managers.Add(type, comp);

            if (comp is IManager mgr)
                mgr.OnInit();

            return comp;
        }

        #endregion

        #region Get

        public static T GetManager<T>() where T : class
        {
            Type type = typeof(T);

            if (managers.TryGetValue(type, out var obj))
                return obj as T;

            return null;
        }

        #endregion

        #region Remove

        public static void RemoveManager<T>()
        {
            Type type = typeof(T);

            if (!managers.TryGetValue(type, out var obj))
                return;

            if (obj is IManager mgr)
                mgr.OnDispose();

            if (obj is Component comp)
                GameObject.Destroy(comp);

            managers.Remove(type);
        }

        #endregion

        #region Clear

        public static void Clear()
        {
            foreach (var kv in managers)
            {
                if (kv.Value is IManager mgr)
                    mgr.OnDispose();

                if (kv.Value is Component comp)
                    GameObject.Destroy(comp);
            }

            managers.Clear();
        }

        #endregion
    }
}