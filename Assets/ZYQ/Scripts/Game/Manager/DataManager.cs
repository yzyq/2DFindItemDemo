using TMPro;
using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    public class DataManager : ManagerBase
    {
        private readonly TMP_FontAsset fontAsset;
        private readonly GameDataConfig gameData;
        private readonly LevelDataSO levelDataSO;
        private LevelDataConfig currentLevel;
        private LevelData currentLevelData;

        public TMP_FontAsset FontAsset => fontAsset;
        public GameDataConfig GameData => gameData;
        public LevelDataConfig CurrentLevel => currentLevel;
        public LevelData CurrentLevelData => currentLevelData;
        public string CurrentLevelId => currentLevel?.levelId;
        public IReadOnlyList<LevelDataConfig> Levels => gameData.levels;
        public IReadOnlyList<LevelData> LevelDataList => levelDataSO != null ? levelDataSO.Levels : null;
        public HiddenObjectDemoConfig DemoConfig => currentLevel?.hiddenObjectConfig ?? new HiddenObjectDemoConfig();

        public DataManager(TMP_FontAsset fontAsset, GameDataConfig gameData, HiddenObjectDemoConfig fallbackDemoConfig)
            : this(fontAsset, gameData, fallbackDemoConfig, null)
        {
        }

        public DataManager(TMP_FontAsset fontAsset, GameDataConfig gameData, HiddenObjectDemoConfig fallbackDemoConfig, LevelDataSO levelDataSO)
        {
            this.fontAsset = fontAsset;
            this.gameData = gameData ?? new GameDataConfig();
            this.levelDataSO = levelDataSO;
            NormalizeGameData(fallbackDemoConfig);
        }

        protected override void OnInit()
        {
            LoadLevel(gameData.defaultLevelId);
        }

        public bool LoadLevel(string levelId)
        {
            currentLevel = gameData.GetLevel(levelId);
            if (currentLevel == null)
            {
                Debug.LogError($"[DataManager] Level not found: {levelId}");
                return false;
            }

            currentLevel.hiddenObjectConfig ??= new HiddenObjectDemoConfig();
            currentLevel.hiddenObjectConfig.Clamp();
            currentLevelData = ResolveLevelData(levelId);
            ApplyLevelDataToDemoConfig(currentLevelData, currentLevel.hiddenObjectConfig);
            return true;
        }

        public LevelData GetLevelData(int levelId)
        {
            return levelDataSO != null ? levelDataSO.GetLevel(levelId) : null;
        }

        public UIPanel LoadPanelPrefab(UIPanelType type)
        {
            var config = gameData.GetPanel(type);
            if (config == null) return null;

            if (config.prefab != null)
                return config.prefab;

            if (!string.IsNullOrWhiteSpace(config.resourcesPath))
                return Resources.Load<UIPanel>(config.resourcesPath);

            return null;
        }

        private void NormalizeGameData(HiddenObjectDemoConfig fallbackDemoConfig)
        {
            gameData.uiPanels ??= new System.Collections.Generic.List<UIPanelPrefabConfig>();
            gameData.levels ??= new System.Collections.Generic.List<LevelDataConfig>();

            if (gameData.levels.Count == 0)
            {
                gameData.levels.Add(new LevelDataConfig
                {
                    levelId = string.IsNullOrEmpty(gameData.defaultLevelId) ? "level_001" : gameData.defaultLevelId,
                    displayName = "Default",
                    hiddenObjectConfig = fallbackDemoConfig ?? new HiddenObjectDemoConfig()
                });
            }

            foreach (var level in gameData.levels)
            {
                if (level == null) continue;

                if (string.IsNullOrEmpty(level.levelId))
                    level.levelId = "level_" + gameData.levels.IndexOf(level).ToString("000");

                level.hiddenObjectConfig ??= new HiddenObjectDemoConfig();
                level.hiddenObjectConfig.Clamp();
            }

            if (string.IsNullOrEmpty(gameData.defaultLevelId) || gameData.GetLevel(gameData.defaultLevelId) == null)
                gameData.defaultLevelId = gameData.levels[0].levelId;
        }

        private LevelData ResolveLevelData(string levelId)
        {
            if (levelDataSO == null || levelDataSO.Levels == null || levelDataSO.Levels.Count == 0)
                return null;

            if (TryParseLevelNumber(levelId, out int numericLevelId))
            {
                var matched = levelDataSO.GetLevel(numericLevelId);
                if (matched != null)
                    return matched;
            }

            return levelDataSO.Levels[0];
        }

        private static void ApplyLevelDataToDemoConfig(LevelData levelData, HiddenObjectDemoConfig config)
        {
            if (levelData == null || config == null) return;

            if (levelData.Foreground != null)
                config.lineArtMap = levelData.Foreground;

            if (levelData.Background != null)
                config.colorMap = levelData.Background;

            config.Clamp();
        }

        private static bool TryParseLevelNumber(string levelId, out int numericLevelId)
        {
            numericLevelId = 0;
            if (string.IsNullOrWhiteSpace(levelId))
                return false;

            if (int.TryParse(levelId, out numericLevelId))
                return true;

            int start = levelId.Length;
            while (start > 0 && char.IsDigit(levelId[start - 1]))
                start--;

            return start < levelId.Length && int.TryParse(levelId.Substring(start), out numericLevelId);
        }
    }
}
