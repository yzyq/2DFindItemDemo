using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    [Serializable]
    public class HiddenObjectDemoConfig
    {
        [Header("Assets")]
        public Sprite companyLogo;
        public Sprite lineArtMap;
        public Sprite colorMap;
        public Sprite cloudFill;
        public Sprite findBarBackground;
        public Sprite findItemFrame;
        public Sprite checkMark;

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

        [Header("Targets")]
        public List<HiddenObjectTargetConfig> targets = new();

        public void Clamp()
        {
            if (initialViewHeightRatio <= 0f)
                initialViewHeightRatio = 0.65f;

            initialViewHeightRatio = Mathf.Clamp(initialViewHeightRatio, 0.25f, 1f);
            minZoom = Mathf.Min(minZoom, defaultZoom);
            maxZoom = Mathf.Max(maxZoom, defaultZoom);
            spotlightRadiusScreenRatio = Mathf.Clamp(spotlightRadiusScreenRatio, 0.05f, 0.6f);
            spotlightLeakRatio = Mathf.Clamp01(spotlightLeakRatio);
            reboundTime = Mathf.Max(0.01f, reboundTime);
            idleReplayInterval = Mathf.Max(0f, idleReplayInterval);
            collectFlyDuration = Mathf.Max(0.05f, collectFlyDuration);
        }
    }

    [Serializable]
    public class HiddenObjectTargetConfig
    {
        public string id;
        public string displayName;
        public Sprite worldSprite;
        public Sprite iconSprite;
        public Vector2 normalizedPosition = new(0.5f, 0.5f);
        public Vector2 worldSize = new(0.9f, 0.9f);
        public bool hasInteractAnimation = true;
        public Spine.Unity.SkeletonDataAsset skeletonData;
        public string idleAnimationName;
        public string interactAnimationName;
        public AudioClip correctClip;
        public AudioClip wrongClip;
    }
}
