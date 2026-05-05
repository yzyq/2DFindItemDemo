using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    [Serializable]
    public class UIPanelPrefabConfig
    {
        public UIPanelType type;

        [Tooltip("Direct prefab reference. Preferred for editor-assigned UI prefabs.")]
        public UIPanel prefab;

        [Tooltip("Optional Resources path, for example: UI/UILoadingPanel. Used when prefab is empty.")]
        public string resourcesPath;
    }

    [Serializable]
    public class LevelDataConfig
    {
        public string levelId = "level_001";
        public string displayName = "Level 1";
        public HiddenObjectDemoConfig hiddenObjectConfig = new();
    }

    [Serializable]
    public class GameDataConfig
    {
        public string defaultLevelId = "level_001";
        public List<UIPanelPrefabConfig> uiPanels = new();
        public List<LevelDataConfig> levels = new();

        public LevelDataConfig GetLevel(string levelId)
        {
            if (!string.IsNullOrEmpty(levelId))
            {
                foreach (var level in levels)
                {
                    if (level != null && level.levelId == levelId)
                        return level;
                }
            }

            return levels.Count > 0 ? levels[0] : null;
        }

        public UIPanelPrefabConfig GetPanel(UIPanelType type)
        {
            foreach (var panel in uiPanels)
            {
                if (panel != null && panel.type == type)
                    return panel;
            }

            return null;
        }
    }
}
