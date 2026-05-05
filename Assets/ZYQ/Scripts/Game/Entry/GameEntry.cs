using TMPro;
using UnityEngine;
using System.Collections.Generic;

namespace ZYQ.Demo
{
    public class GameEntry : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset fontAsset;
        [SerializeField] private HiddenObjectCommonConfigAsset commonConfigAsset;
        [SerializeField] private List<HiddenObjectLevelConfigAsset> levelConfigAssets = new();
        [SerializeField] private LevelDataSO levelDataSO;
        [SerializeField] private GameDataConfig gameData = new();
        [SerializeField] private HiddenObjectDemoConfig demoConfig = new();

        private AppContext context;

        void Awake()
        {
            context = new AppContext();

            Register(new DataManager(GetFontAsset(), BuildGameData(), demoConfig, levelDataSO));
            Register(new UIManager());
            Register(new GameManager());
            Register(new InputManager());
            Register(new MatchSystem());
            Register(new SceneLoader());

            context.InitializeAll();
        }

        void Update()
        {
            context?.TickAll(Time.deltaTime);
            EventDispatcher.Tick();
        }

        void OnDestroy()
        {
            context?.DisposeAll();
            EventDispatcher.Clear();
            context = null;
        }

        void Register<T>(T mgr) where T : ManagerBase
        {
            context.Register(mgr);
        }

        TMP_FontAsset GetFontAsset()
        {
            return fontAsset != null ? fontAsset : commonConfigAsset != null ? commonConfigAsset.fontAsset : null;
        }

        GameDataConfig BuildGameData()
        {
            var data = CloneGameData(gameData);

            if (commonConfigAsset != null)
                data.uiPanels = new List<UIPanelPrefabConfig>(commonConfigAsset.uiPanels);

            if (levelConfigAssets != null && levelConfigAssets.Count > 0)
            {
                data.levels = new List<LevelDataConfig>();
                foreach (var levelAsset in levelConfigAssets)
                {
                    if (levelAsset == null) continue;
                    data.levels.Add(levelAsset.ToLevelData(commonConfigAsset));
                }

                if (string.IsNullOrEmpty(data.defaultLevelId) && data.levels.Count > 0)
                    data.defaultLevelId = data.levels[0].levelId;
            }
            else if (data.levels.Count == 0)
            {
                data.levels.Add(new LevelDataConfig
                {
                    levelId = string.IsNullOrEmpty(data.defaultLevelId) ? "level_001" : data.defaultLevelId,
                    displayName = "Default",
                    hiddenObjectConfig = BuildFallbackDemoConfig()
                });
            }

            return data;
        }

        HiddenObjectDemoConfig BuildFallbackDemoConfig()
        {
            var config = CloneDemoConfig(demoConfig);
            commonConfigAsset?.ApplyTo(config);
            config.Clamp();
            return config;
        }

        GameDataConfig CloneGameData(GameDataConfig source)
        {
            var result = new GameDataConfig();
            if (source == null)
                return result;

            result.defaultLevelId = source.defaultLevelId;
            result.uiPanels = source.uiPanels != null
                ? new List<UIPanelPrefabConfig>(source.uiPanels)
                : new List<UIPanelPrefabConfig>();

            result.levels = new List<LevelDataConfig>();
            if (source.levels != null)
            {
                foreach (var level in source.levels)
                {
                    if (level == null) continue;
                    result.levels.Add(new LevelDataConfig
                    {
                        levelId = level.levelId,
                        displayName = level.displayName,
                        hiddenObjectConfig = CloneDemoConfig(level.hiddenObjectConfig)
                    });
                }
            }

            return result;
        }

        HiddenObjectDemoConfig CloneDemoConfig(HiddenObjectDemoConfig source)
        {
            var result = new HiddenObjectDemoConfig();
            if (source == null)
                return result;

            result.companyLogo = source.companyLogo;
            result.lineArtMap = source.lineArtMap;
            result.colorMap = source.colorMap;
            result.cloudFill = source.cloudFill;
            result.findBarBackground = source.findBarBackground;
            result.findItemFrame = source.findItemFrame;
            result.checkMark = source.checkMark;
            result.spotlightRadiusScreenRatio = source.spotlightRadiusScreenRatio;
            result.spotlightLeakRatio = source.spotlightLeakRatio;
            result.spotlightEnabledByDefault = source.spotlightEnabledByDefault;
            result.mapDragSpeed = source.mapDragSpeed;
            result.spotlightDragSpeed = source.spotlightDragSpeed;
            result.edgeFollowSpeed = source.edgeFollowSpeed;
            result.edgeFollowPadding = source.edgeFollowPadding;
            result.reboundTime = source.reboundTime;
            result.reboundDamping = source.reboundDamping;
            result.initialViewHeightRatio = source.initialViewHeightRatio;
            result.defaultZoom = source.defaultZoom;
            result.minZoom = source.minZoom;
            result.maxZoom = source.maxZoom;
            result.wheelZoomSpeed = source.wheelZoomSpeed;
            result.pinchZoomSpeed = source.pinchZoomSpeed;
            result.idleReplayInterval = source.idleReplayInterval;
            result.idleAnimationName = source.idleAnimationName;
            result.interactAnimationName = source.interactAnimationName;
            result.backgroundClip = source.backgroundClip;
            result.defaultCorrectClip = source.defaultCorrectClip;
            result.defaultWrongClip = source.defaultWrongClip;
            result.wrongShakeDistance = source.wrongShakeDistance;
            result.wrongShakeDuration = source.wrongShakeDuration;
            result.collectFlyDuration = source.collectFlyDuration;
            result.collectPopScale = source.collectPopScale;
            result.targets = CloneTargets(source.targets);
            result.Clamp();
            return result;
        }

        List<HiddenObjectTargetConfig> CloneTargets(List<HiddenObjectTargetConfig> source)
        {
            var result = new List<HiddenObjectTargetConfig>();
            if (source == null)
                return result;

            foreach (var item in source)
            {
                if (item == null) continue;
                result.Add(new HiddenObjectTargetConfig
                {
                    id = item.id,
                    displayName = item.displayName,
                    worldSprite = item.worldSprite,
                    iconSprite = item.iconSprite,
                    normalizedPosition = item.normalizedPosition,
                    worldSize = item.worldSize,
                    hasInteractAnimation = item.hasInteractAnimation,
                    skeletonData = item.skeletonData,
                    idleAnimationName = item.idleAnimationName,
                    interactAnimationName = item.interactAnimationName,
                    correctClip = item.correctClip,
                    wrongClip = item.wrongClip
                });
            }

            return result;
        }

    }
}
