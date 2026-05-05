using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace ZYQ.Demo
{
    public class SceneLoader : ManagerBase
    {
        private bool isLoading;

        protected override void OnInit()
        {
            isLoading = false;
        }

        public async UniTask LoadSceneAsync(string sceneName, Action onBeforeLoad = null, Action onAfterLoad = null)
        {
            if (isLoading) return;
            isLoading = true;

            try
            {
                // 🔥 1. 进入加载前清理系统
                onBeforeLoad?.Invoke();

                await ClearSystems();

                // 🔥 2. 可选：加载UI Loading
                await LoadLoadingUI();

                // 🔥 3. 异步加载场景
                var op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null) return;

                op.allowSceneActivation = false;

                while (op.progress < 0.9f)
                {
                    await UniTask.Yield();
                }

                op.allowSceneActivation = true;

                while (!op.isDone)
                {
                    await UniTask.Yield();
                }

                // 🔥 4. 等一帧确保Scene初始化
                await UniTask.Yield();

                // 🔥 5. 重建系统
                await RebuildSystems();

                onAfterLoad?.Invoke();
            }
            finally
            {
                isLoading = false;
            }
        }

        // =========================
        // 清理系统
        // =========================
        private UniTask ClearSystems()
        {
            EventDispatcher.Clear();
            return UniTask.CompletedTask;
        }

        // =========================
        // 加载UI过渡（可扩展）
        // =========================
        private UniTask LoadLoadingUI()
        {
            // 可接 UIManager.Show<LoadingPanel>()
            return UniTask.CompletedTask;
        }

        // =========================
        // 重建系统
        // =========================
        private UniTask RebuildSystems()
        {
            // 重新绑定 Canvas / UI / Input

            Context.Reinitialize<UIManager>();
            Context.Reinitialize<GameManager>();
            Context.Reinitialize<InputManager>();
            Context.Reinitialize<MatchSystem>();

            return UniTask.CompletedTask;
        }

        protected override void OnDispose()
        {
            isLoading = false;
        }
    }
}
