using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZYQ.Demo
{
    public class GameManager : ManagerBase
    {
        private readonly HiddenObjectDemoConfig overrideDemoConfig;
        private Map map;
        private UILoginPanel loginPanel;
        private UIMainPanel mainPanel;

        public GameManager()
        {
        }

        public GameManager(HiddenObjectDemoConfig demoConfig)
        {
            overrideDemoConfig = demoConfig;
        }

        protected override void OnInit()
        {
            Debug.Log("GameManager Init");

            map = ResolveSceneMap();
            map.Initialize(Context, ResolveConfig());
            ShowStartPageAsync().Forget();
        }

        protected override void OnDispose()
        {
            UnbindLoginPanel();
            UnbindMainPanel();

            if (map != null)
            {
                map.DisposeMap();
                map = null;
            }

            loginPanel = null;
            mainPanel = null;

            Debug.Log("GameManager OnDispose");
        }

        public override void Tick(float dt)
        {
            map?.Tick(dt);
        }

        private HiddenObjectDemoConfig ResolveConfig()
        {
            if (overrideDemoConfig != null)
                return overrideDemoConfig;

            return Context.TryGet(out DataManager dataManager)
                ? dataManager.DemoConfig
                : new HiddenObjectDemoConfig();
        }

        private Map ResolveSceneMap()
        {
            var sceneMap = Object.FindFirstObjectByType<Map>(FindObjectsInactive.Include);
            if (sceneMap != null)
                return sceneMap;

            var mapRoot = GameObject.Find("MapRoot");
            if (mapRoot == null)
            {
                Debug.LogWarning("GameManager: 场景中未找到 MapRoot，将创建空节点作为兜底。");
                mapRoot = new GameObject("MapRoot");
            }

            return mapRoot.GetComponent<Map>() ?? mapRoot.AddComponent<Map>();
        }

        private async UniTask ShowStartPageAsync()
        {
            map?.SetRunning(false);
            UnbindMainPanel();

            if (mainPanel != null)
                await mainPanel.HideAsync();

            if (!Context.TryGet(out UIManager uiManager)) return;

            UnbindLoginPanel();
            loginPanel = await uiManager.Open<UILoginPanel>(UIPanelType.Start);
            BindLoginPanel();
        }

        private async UniTask ShowGamePageAsync()
        {
            if (loginPanel != null)
            {
                UnbindLoginPanel();
                await loginPanel.HideAsync();
            }

            if (Context.TryGet(out UIManager uiManager))
            {
                mainPanel = await uiManager.Open<UIMainPanel>(UIPanelType.Game);
                BindMainPanel();
            }

            map?.SetMainPanel(mainPanel);
            map?.SetRunning(true);
        }

        private void BindLoginPanel()
        {
            if (loginPanel == null) return;

            loginPanel.StartClicked -= HandleLoginStartClicked;
            loginPanel.StartClicked += HandleLoginStartClicked;
        }

        private void UnbindLoginPanel()
        {
            if (loginPanel == null) return;
            loginPanel.StartClicked -= HandleLoginStartClicked;
        }

        private void BindMainPanel()
        {
            if (mainPanel == null) return;

            mainPanel.ReturnClicked -= HandleMainReturnClicked;
            mainPanel.ResetClicked -= HandleResetClicked;
            mainPanel.SpotlightClicked -= HandleSpotlightClicked;
            mainPanel.OtherClicked -= HandleOtherClicked;

            mainPanel.ReturnClicked += HandleMainReturnClicked;
            mainPanel.ResetClicked += HandleResetClicked;
            mainPanel.SpotlightClicked += HandleSpotlightClicked;
            mainPanel.OtherClicked += HandleOtherClicked;
        }

        private void UnbindMainPanel()
        {
            if (mainPanel == null) return;

            mainPanel.ReturnClicked -= HandleMainReturnClicked;
            mainPanel.ResetClicked -= HandleResetClicked;
            mainPanel.SpotlightClicked -= HandleSpotlightClicked;
            mainPanel.OtherClicked -= HandleOtherClicked;
        }

        private void HandleLoginStartClicked()
        {
            ShowGamePageAsync().Forget();
        }

        private void HandleMainReturnClicked()
        {
            ShowStartPageAsync().Forget();
        }

        private void HandleResetClicked()
        {
            map?.ResetGame();
        }

        private void HandleSpotlightClicked()
        {
            map?.ToggleSpotlight();
        }

        private void HandleOtherClicked()
        {
            Debug.Log("Main panel other clicked.");
        }
    }
}
