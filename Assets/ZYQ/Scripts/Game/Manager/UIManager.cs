using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ZYQ.Demo
{
    public class UIManager : ManagerBase
    {
        private UIPanelFactory factory;
        private UIPanelPool pool;
        private UIStack stack;
        private readonly Dictionary<UIPanelType, UIPanel> scenePanels = new();

        protected override void OnInit()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                canvas = CreateDefaultCanvas();

            factory = new UIPanelFactory(canvas, Context);
            pool = new UIPanelPool();
            stack = new UIStack();
            EnsureInputSystemEventModule();
            RegisterScenePanels();
        }

        public async UniTask<T> Open<T>(UIPanelType type) where T : UIPanel
        {
            var panel = GetScenePanel<T>(type) ?? pool.Get<T>(type) ?? factory.Create<T>(type);
            if (panel == null) return null;

            await panel.ShowAsync();
            stack.Push(panel);

            return panel;
        }

        public async UniTask CloseTop()
        {
            var panel = stack.Pop();
            if (panel == null) return;

            await panel.HideAsync();

            if (!scenePanels.ContainsValue(panel))
                pool.Recycle(panel);
        }

        protected override void OnDispose()
        {
            factory = null;
            pool = null;
            stack = null;
            scenePanels.Clear();
        }

        public void RegisterScenePanel(UIPanel panel)
        {
            if (panel == null) return;

            panel.Bind(Context);
            scenePanels[panel.Type] = panel;
            panel.gameObject.SetActive(false);
        }

        private void RegisterScenePanels()
        {
            scenePanels.Clear();
            var panels = Object.FindObjectsByType<UIPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var panel in panels)
                RegisterScenePanel(panel);
        }

        private T GetScenePanel<T>(UIPanelType type) where T : UIPanel
        {
            if (!scenePanels.TryGetValue(type, out var panel))
                return null;

            return panel as T;
        }

        private Canvas CreateDefaultCanvas()
        {
            var go = new GameObject("[UICanvas]");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void EnsureInputSystemEventModule()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule != null)
                Object.Destroy(standaloneModule);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }
}
