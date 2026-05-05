using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    public enum UIPanelType
    {
        Loading,
        Start,
        Game
    }
    public class UIPanelFactory
    {
        private Canvas root;
        private AppContext context;
        private Dictionary<UIPanelType, UIPanel> prefabs;
        private DataManager dataManager;

        public UIPanelFactory(Canvas root, AppContext context)
        {
            this.root = root;
            this.context = context;
            context.TryGet(out dataManager);

            prefabs = new();
        }

        public void Register(UIPanelType type, UIPanel prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("Cannot register null UIPanel prefab: " + type);
                return;
            }

            prefabs[type] = prefab;
        }

        public T Create<T>(UIPanelType type) where T : UIPanel
        {
            var prefab = GetPrefab(type);
            if (prefab == null)
            {
                Debug.LogError("Prefab not found: " + type);
                return null;
            }

            var panel = GameObject.Instantiate(prefab, root.transform);
            panel.Type = type;
            panel.Bind(context);

            return panel as T;
        }

        private UIPanel GetPrefab(UIPanelType type)
        {
            if (prefabs.TryGetValue(type, out var prefab))
                return prefab;

            prefab = dataManager?.LoadPanelPrefab(type);
            if (prefab != null)
                prefabs[type] = prefab;

            return prefab;
        }
    }
}
