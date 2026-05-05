using System;
using Cysharp.Threading.Tasks;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace ZYQ.Demo
{
    public class SpotlightSpineView : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [Header("Animation")]
        [SpineAnimation(dataField: nameof(skeletonAnimation), includeNone: false, fallbackToTextField: true)]
        [SerializeField] private string idleAnimationName;
        [SpineAnimation(dataField: nameof(skeletonAnimation), includeNone: true, fallbackToTextField: true)]
        [SerializeField] private string interactAnimationName;
        [SerializeField] private bool hasInteractAnimation = true;

        [Header("Audio")]
        [SerializeField] private AudioClip interactClip;

        [Header("Debug")]
        [SerializeField] private bool logAnimationWarnings;

        private HiddenObjectDemoConfig config;
        private SkeletonRenderer skeletonRenderer;
        private Renderer boundsRenderer;
        private bool spotlightInside;
        private bool playingInteract;
        private bool animationStopped = true;
        private int stateVersion;

        /// <summary>
        /// 如果配置了交互动画，则返回交互音效，否则返回null
        /// </summary>
        public AudioClip InteractClip => interactClip;

        public void Bind(HiddenObjectDemoConfig config)
        {
            this.config = config;
            skeletonAnimation ??= GetComponent<SkeletonAnimation>();
            skeletonAnimation ??= GetComponentInChildren<SkeletonAnimation>(true);
            skeletonRenderer = GetComponent<SkeletonRenderer>() ?? GetComponentInChildren<SkeletonRenderer>(true);

            if (skeletonRenderer != null)
            {
                skeletonRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                boundsRenderer = skeletonRenderer.GetComponent<Renderer>();
            }

            InitializeSkeleton();
            StopAnimation();
            SetVisible(false);
        }

        public void ResetState()
        {
            stateVersion++;
            spotlightInside = false;
            playingInteract = false;
            StopAnimation();
            SetVisible(false);
        }

        public void SetSpotlightState(bool inside)
        {
            if (inside == spotlightInside)
                return;

            if (inside)
            {
                SetVisible(true);
                PlayIdle();
            }
            else
            {
                StopAnimation();
                SetVisible(false);
            }

            spotlightInside = inside;
        }

        public bool IsLitBySpotlight(Vector3 spotlightWorldPosition, float spotlightRadius)
        {
            if (spotlightRadius <= 0f)
                return false;

            if (boundsRenderer != null)
            {
                var bounds = boundsRenderer.bounds;
                if (bounds.size.sqrMagnitude > 0.0001f)
                {
                    var closest = bounds.ClosestPoint(spotlightWorldPosition);
                    return ((Vector2)closest - (Vector2)spotlightWorldPosition).sqrMagnitude <= spotlightRadius * spotlightRadius;
                }
            }

            return ((Vector2)transform.position - (Vector2)spotlightWorldPosition).sqrMagnitude <= spotlightRadius * spotlightRadius;
        }

        public void ReplayIdleIfLit()
        {
            if (spotlightInside && !playingInteract)
                PlayIdle();
        }

        public bool ContainsWorldPoint(Vector3 worldPoint)
        {
            if (!spotlightInside)
                return false;

            if (boundsRenderer != null)
            {
                var bounds = boundsRenderer.bounds;
                bounds.Expand(0.2f);
                bounds.center = new Vector3(bounds.center.x, bounds.center.y, 0f);
                return bounds.Contains(worldPoint);
            }

            return Vector2.Distance(worldPoint, transform.position) <= 0.8f;
        }

        public async UniTask<bool> TryInteract(Action<SpotlightSpineView> onAccepted = null)
        {
            if (!spotlightInside || playingInteract)
                return false;

            playingInteract = true;
            int version = stateVersion;

            try
            {
                onAccepted?.Invoke(this);

                if (hasInteractAnimation && HasAnimation(GetInteractName()))
                    await PlayAnimationOnce(GetInteractName());
                else
                    PlayIdle();
            }
            finally
            {
                playingInteract = false;

                if (version == stateVersion && spotlightInside && gameObject.activeInHierarchy)
                    PlayIdle();
            }

            return true;
        }

        private void SetVisible(bool visible)
        {
            if (skeletonAnimation != null)
                skeletonAnimation.enabled = visible;

            if (boundsRenderer != null)
            {
                boundsRenderer.enabled = visible;
                return;
            }

            if (skeletonAnimation != null && skeletonAnimation.gameObject != gameObject)
                skeletonAnimation.gameObject.SetActive(visible);
        }

        private void PlayIdle()
        {
            PlayAnimation(GetIdleName(), true);
        }

        private TrackEntry PlayAnimation(string animationName, bool loop)
        {
            if (skeletonAnimation == null || string.IsNullOrWhiteSpace(animationName))
                return null;

            animationStopped = false;
            skeletonAnimation.enabled = true;
            InitializeSkeleton();

            if (skeletonAnimation.AnimationState == null)
                return null;

            try
            {
                return skeletonAnimation.AnimationState.SetAnimation(0, animationName, loop);
            }
            catch (Exception exception)
            {
                if (logAnimationWarnings)
                    Debug.LogWarning($"SpotlightSpineView: 播放 Spine 动画失败。对象={name}, animation={animationName}, error={exception.Message}", this);

                return null;
            }
        }

        private void StopAnimation()
        {
            if (skeletonAnimation == null)
                return;

            if (animationStopped && !skeletonAnimation.enabled)
                return;

            InitializeSkeleton();
            skeletonAnimation.AnimationState?.ClearTracks();
            skeletonAnimation.Skeleton?.SetToSetupPose();
            skeletonAnimation.Update(0f);
            skeletonAnimation.enabled = false;
            animationStopped = true;
        }

        private bool HasAnimation(string animationName)
        {
            if (skeletonAnimation == null || string.IsNullOrWhiteSpace(animationName))
                return false;

            InitializeSkeleton();
            return skeletonAnimation.Skeleton?.Data?.FindAnimation(animationName) != null;
        }

        private void InitializeSkeleton()
        {
            if (skeletonAnimation == null)
                return;

            if (!skeletonAnimation.valid)
                skeletonAnimation.Initialize(false);
        }

        private async UniTask PlayAnimationOnce(string animationName)
        {
            var entry = PlayAnimation(animationName, false);
            if (entry == null)
            {
                await UniTask.Delay(450);
                return;
            }

            bool completed = false;
            void HandleComplete(TrackEntry _) => completed = true;

            entry.Complete += HandleComplete;

            try
            {
                float duration = Mathf.Max(0.1f, entry.AnimationEnd - entry.AnimationStart);
                float elapsed = 0f;

                while (!completed && elapsed < duration + 0.15f)
                {
                    if (!spotlightInside)
                        break;

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
            if (!string.IsNullOrWhiteSpace(idleAnimationName))
                return idleAnimationName;

            return config != null && !string.IsNullOrWhiteSpace(config.idleAnimationName)
                ? config.idleAnimationName
                : "idle";
        }

        private string GetInteractName()
        {
            if (!string.IsNullOrWhiteSpace(interactAnimationName))
                return interactAnimationName;

            return config != null && !string.IsNullOrWhiteSpace(config.interactAnimationName)
                ? config.interactAnimationName
                : "animation";
        }
    }
}
