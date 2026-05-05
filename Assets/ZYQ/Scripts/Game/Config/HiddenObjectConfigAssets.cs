using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    [CreateAssetMenu(fileName = "HiddenObjectCommonConfig", menuName = "ZYQ/Hidden Object/Common Config")]
    public class HiddenObjectCommonConfigAsset : ScriptableObject
    {
        [Header("UI")]
        public TMPro.TMP_FontAsset fontAsset;
        public List<UIPanelPrefabConfig> uiPanels = new();

        [Header("Spotlight")]
        [Range(0.05f, 0.6f)] public float spotlightRadiusScreenRatio = 0.25f;
        [Range(0f, 0.8f)] public float spotlightLeakRatio = 0.35f;
        public bool spotlightEnabledByDefault = true;

        [Header("Map Feel")]
        public float mapDragSpeed = 1f;
        public float spotlightDragSpeed = 1f;
        public float edgeFollowSpeed = 5f;
        public float edgeFollowPadding = 72f;
        public float reboundTime = 0.22f;
        public float reboundDamping = 12f;

        [Header("Zoom")]
        [Range(0.25f, 1f)] public float initialViewHeightRatio = 0.65f;
        public float defaultZoom = 1f;
        public float minZoom = 0.7f;
        public float maxZoom = 2f;
        public float wheelZoomSpeed = 0.08f;
        public float pinchZoomSpeed = 0.005f;

        [Header("Spine")]
        public float idleReplayInterval = 0.2f;
        public string idleAnimationName = "idle";
        public string interactAnimationName = "animation";

        [Header("Feedback")]
        public AudioClip backgroundClip;
        public AudioClip defaultCorrectClip;
        public AudioClip defaultWrongClip;
        public float wrongShakeDistance = 10f;
        public float wrongShakeDuration = 0.18f;
        public float collectFlyDuration = 0.55f;
        public float collectPopScale = 1.18f;

        public void ApplyTo(HiddenObjectDemoConfig config)
        {
            if (config == null) return;

            config.spotlightRadiusScreenRatio = spotlightRadiusScreenRatio;
            config.spotlightLeakRatio = spotlightLeakRatio;
            config.spotlightEnabledByDefault = spotlightEnabledByDefault;
            config.mapDragSpeed = mapDragSpeed;
            config.spotlightDragSpeed = spotlightDragSpeed;
            config.edgeFollowSpeed = edgeFollowSpeed;
            config.edgeFollowPadding = edgeFollowPadding;
            config.reboundTime = reboundTime;
            config.reboundDamping = reboundDamping;
            config.initialViewHeightRatio = initialViewHeightRatio;
            config.defaultZoom = defaultZoom;
            config.minZoom = minZoom;
            config.maxZoom = maxZoom;
            config.wheelZoomSpeed = wheelZoomSpeed;
            config.pinchZoomSpeed = pinchZoomSpeed;
            config.idleReplayInterval = idleReplayInterval;
            config.idleAnimationName = idleAnimationName;
            config.interactAnimationName = interactAnimationName;
            config.backgroundClip = backgroundClip;
            config.defaultCorrectClip = defaultCorrectClip;
            config.defaultWrongClip = defaultWrongClip;
            config.wrongShakeDistance = wrongShakeDistance;
            config.wrongShakeDuration = wrongShakeDuration;
            config.collectFlyDuration = collectFlyDuration;
            config.collectPopScale = collectPopScale;
            config.Clamp();
        }
    }

    [CreateAssetMenu(fileName = "HiddenObjectLevelConfig", menuName = "ZYQ/Hidden Object/Level Config")]
    public class HiddenObjectLevelConfigAsset : ScriptableObject
    {
        public string levelId = "level_001";
        public string displayName = "Level 1";

        [Header("Level Assets")]
        public Sprite companyLogo;
        public Sprite lineArtMap;
        public Sprite colorMap;
        public Sprite cloudFill;
        public Sprite findBarBackground;
        public Sprite findItemFrame;
        public Sprite checkMark;

        [Header("Targets")]
        public List<HiddenObjectTargetConfig> targets = new();

        public LevelDataConfig ToLevelData(HiddenObjectCommonConfigAsset commonConfig)
        {
            var config = new HiddenObjectDemoConfig
            {
                companyLogo = companyLogo,
                lineArtMap = lineArtMap,
                colorMap = colorMap,
                cloudFill = cloudFill,
                findBarBackground = findBarBackground,
                findItemFrame = findItemFrame,
                checkMark = checkMark,
                targets = CopyTargets(targets)
            };

            commonConfig?.ApplyTo(config);
            config.Clamp();

            return new LevelDataConfig
            {
                levelId = levelId,
                displayName = displayName,
                hiddenObjectConfig = config
            };
        }

        private static List<HiddenObjectTargetConfig> CopyTargets(List<HiddenObjectTargetConfig> source)
        {
            var result = new List<HiddenObjectTargetConfig>();
            if (source == null) return result;

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
