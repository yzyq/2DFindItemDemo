using System;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ZYQ.Demo
{
    public class HiddenObjectTargetView : MonoBehaviour
    {
        [SerializeField] AudioClip interactClip;

        private HiddenObjectTargetConfig config;
        private HiddenObjectDemoConfig tuning;
        private SpriteRenderer spriteRenderer;
        private SkeletonAnimation skeletonAnimation;
        private SkeletonRenderer skeletonRenderer;
        private Renderer boundsRenderer;
        private bool completed;
        private bool spotlightInside;
        private bool playingInteract;
        private float lastIdleTime = -999f;
        private int stateVersion;

        public string Id => config != null ? config.id : name;
        public bool Completed => completed;
        public Vector3 WorldPosition => transform.position;
        public HiddenObjectTargetConfig Config => config;
        public AudioClip InteractClip => interactClip;

        public void Bind(HiddenObjectTargetConfig config, HiddenObjectDemoConfig tuning)
        {
            this.config = config;
            this.tuning = tuning;

            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null && config.worldSprite != null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                if (spriteRenderer.sprite == null && config.worldSprite != null)
                    spriteRenderer.sprite = config.worldSprite;

                spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                boundsRenderer = spriteRenderer;
            }

            if (config.worldSize.x > 0f && config.worldSize.y > 0f && transform.localScale == Vector3.one)
                transform.localScale = new Vector3(config.worldSize.x, config.worldSize.y, 1f);

            skeletonAnimation = GetComponentInChildren<SkeletonAnimation>(true);
            skeletonRenderer = GetComponentInChildren<SkeletonRenderer>(true);

            if (skeletonAnimation == null && config.skeletonData != null)
            {
                var spineGo = new GameObject("Spine");
                spineGo.transform.SetParent(transform, false);
                skeletonAnimation = spineGo.AddComponent<SkeletonAnimation>();
                skeletonRenderer = skeletonAnimation;
                skeletonAnimation.skeletonDataAsset = config.skeletonData;
                skeletonAnimation.Initialize(true);
            }
            else if (skeletonAnimation != null && skeletonAnimation.skeletonDataAsset == null && config.skeletonData != null)
            {
                skeletonAnimation.skeletonDataAsset = config.skeletonData;
                skeletonAnimation.Initialize(true);
            }

            if (skeletonRenderer != null)
            {
                skeletonRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                boundsRenderer = skeletonRenderer.GetComponent<Renderer>();
            }

            ResetState();
        }

        public void ResetState()
        {
            stateVersion++;
            completed = false;
            playingInteract = false;
            spotlightInside = false;
            lastIdleTime = -999f;
            gameObject.SetActive(true);
            SetVisible(false);
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            if (!gameObject.activeSelf || completed) return false;
            if (boundsRenderer != null)
            {
                var bounds = boundsRenderer.bounds;
                bounds.Expand(0.15f);
                bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
                if (bounds.Contains(worldPoint))
                    return true;
            }

            var hitRadius = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) * 0.55f;
            return Vector2.Distance(worldPoint, transform.position) <= hitRadius;
        }

        public void SetSpotlightState(bool inside)
        {
            if (completed) return;

            //if (spotlightInside) Debug.Log("====>:" + spotlightInside + "-----Name--->:" + gameObject.name);
            if (inside && !spotlightInside)
                PlayIdle();

            spotlightInside = inside;
            SetVisible(inside);
        }

        public async UniTask<bool> TryInteract(Action<HiddenObjectTargetView> onAccepted = null)
        {
            if (completed || !spotlightInside || playingInteract) return false;

            playingInteract = true;
            int version = stateVersion;
            onAccepted?.Invoke(this);

            if (config.hasInteractAnimation)
            {
                await PlayAnimationOnce(GetInteractName());
            }
            else
            {
                PlayIdle();
                await UniTask.Yield();
            }

            if (version != stateVersion || completed || !gameObject.activeInHierarchy)
                return false;

            playingInteract = false;
            PlayIdle();
            return true;
        }

        public void MarkCompleted()
        {
            completed = true;
            gameObject.SetActive(false);
        }

        private void SetVisible(bool visible)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = visible;
            if (skeletonAnimation != null) skeletonAnimation.gameObject.SetActive(visible);
        }

        private void PlayIdle()
        {
            lastIdleTime = Time.time;
            PlayAnimation(GetIdleName(), true);
        }

        private TrackEntry PlayAnimation(string animationName, bool loop)
        {
            if (skeletonAnimation == null || string.IsNullOrWhiteSpace(animationName)) return null;
            if (skeletonAnimation.AnimationState == null) return null;

            try
            {
                return skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            }
            catch (System.Exception)
            {
                // Demo assets may use different animation names; missing clips should not break interaction.
                return null;
            }
        }

        private async UniTask PlayAnimationOnce(string animationName)
        {
            var entry = PlayAnimation(animationName, false);
            if (entry == null)
            {
                await UniTask.Delay(450);
                return;
            }

            bool completedAnimation = false;
            void HandleComplete(TrackEntry _) => completedAnimation = true;

            entry.Complete += HandleComplete;

            try
            {
                float duration = Mathf.Max(0.1f, entry.AnimationEnd - entry.AnimationStart);
                float elapsed = 0f;

                while (!completedAnimation && elapsed < duration + 0.15f)
                {
                    elapsed += Time.deltaTime;
                    await UniTask.Yield();
                }
            }
            finally
            {
                entry.Complete -= HandleComplete;
            }
        }

        private string GetIdleName()
        {
            return string.IsNullOrWhiteSpace(config.idleAnimationName) ? tuning.idleAnimationName : config.idleAnimationName;
        }

        private string GetInteractName()
        {
            return string.IsNullOrWhiteSpace(config.interactAnimationName) ? tuning.interactAnimationName : config.interactAnimationName;
        }
    }
}
