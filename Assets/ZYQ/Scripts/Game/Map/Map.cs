using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZYQ.Demo
{
    public class Map : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private List<SpriteRenderer> colorMapRenderers = new();
        [SerializeField] private List<SpriteRenderer> lineArtRenderers = new();
        [SerializeField] private List<SpriteRenderer> spotlightVisibleSpriteRenderers = new();
        [SerializeField] private List<SkeletonRenderer> spotlightVisibleSkeletonRenderers = new();
        [SerializeField] private List<SpotlightSpineView> spotlightSpineViews = new();
        [SerializeField] private List<HiddenObjectTargetView> targetViews = new();
        [SerializeField] private SpriteMask spotlightMask;
        [SerializeField] private Transform spotlightVisual;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
        [SerializeField] private bool logBgmState = true;

        private AppContext context;
        private HiddenObjectDemoConfig config;
        private Camera mainCamera;
        private Canvas canvas;
        private UIMainPanel mainPanel;
        private Bounds mapLocalBounds;
        private MapCameraRig cameraRig;
        private Sprite circleSprite;
        private Sprite spotlightGlowSprite;
        private CancellationTokenSource gameplayCts;

        private readonly List<HiddenObjectTargetView> targets = new();
        private readonly List<SpotlightSpineView> spineViews = new();

        private bool initialized;
        private bool running;
        private bool spotlightEnabled;
        private bool draggingSpotlight;
        private bool draggingMap;
        private bool pointerDown;
        private bool pointerMoved;
        private bool lastTouchWasPinch;
        private Vector2 pointerDownScreen;
        private float spotlightRadiusWorld;
        private float lastPinchDistance;
        private float nextEdgeHintTime;

        public void Initialize(AppContext context, HiddenObjectDemoConfig config)
        {
            this.context = context;
            this.config = config ?? new HiddenObjectDemoConfig();
            this.config.Clamp();
            LoadEditorDefaults(this.config);

            BuildCamera();
            BuildSceneBindings();

            gameplayCts = new CancellationTokenSource();
            initialized = true;
            SetRunning(false);
        }

        public void SetMainPanel(UIMainPanel panel)
        {
            mainPanel = panel;
        }

        public void SetRunning(bool value)
        {
            running = value;
            gameObject.SetActive(value);

            if (value)
                spotlightEnabled = config.spotlightEnabledByDefault;

            if (spotlightMask != null)
                spotlightMask.gameObject.SetActive(value && spotlightEnabled);

            if (spotlightVisual != null)
                spotlightVisual.gameObject.SetActive(value && spotlightEnabled);
            if (value)
            {
                PlayLevelBgm();
                ResetGame();
            }
            else
            {
                StopLevelBgm();
            }
        }

        public void Tick(float dt)
        {
            if (!initialized || !running) return;

            UpdateSpotlightRadius();
            HandleInput(dt);
            cameraRig?.Tick(dt);
            UpdateTargetsLighting();
        }

        public void ToggleSpotlight()
        {
            spotlightEnabled = !spotlightEnabled;

            if (spotlightMask != null)
                spotlightMask.gameObject.SetActive(spotlightEnabled);

            if (spotlightVisual != null)
                spotlightVisual.gameObject.SetActive(spotlightEnabled);

            UpdateTargetsLighting();
        }

        public void ResetGame()
        {
            ResetGameplayCancellation();
            ResetMapAndSpotlight();

            foreach (var spineView in spineViews)
                spineView.ResetState();

            foreach (var target in targets)
                target.ResetState();

            UpdateTargetsLighting();
        }

        public void DisposeMap()
        {
            StopLevelBgm();
            CancelAndDisposeGameplayCts();
            targets.Clear();
            mainPanel = null;
            context = null;
            initialized = false;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Collect Scene References")]
        private void AutoCollectSceneReferences()
        {
            lineArtRenderers.Clear();
            colorMapRenderers.Clear();
            spotlightVisibleSpriteRenderers.Clear();
            spotlightVisibleSkeletonRenderers.Clear();
            spotlightSpineViews.Clear();
            targetViews.Clear();

            AddIfNotNull(lineArtRenderers, FindSpriteRendererInMap("LineArt_xiangao"));
            AddIfNotNull(colorMapRenderers, FindSpriteRendererInMap("ColorMap_shangse"));

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer == null || IsInList(lineArtRenderers, renderer) || IsInList(colorMapRenderers, renderer))
                    continue;

                spotlightVisibleSpriteRenderers.Add(renderer);
            }

            foreach (var skeleton in GetComponentsInChildren<SkeletonRenderer>(true))
            {
                spotlightVisibleSkeletonRenderers.Add(skeleton);
                AddIfNotNull(spotlightSpineViews, skeleton.GetComponent<SpotlightSpineView>());
            }

            targetViews.AddRange(GetComponentsInChildren<HiddenObjectTargetView>(true));
            EditorUtility.SetDirty(this);
        }
#endif

        private void BuildCamera()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                mainCamera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            mainCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.88f, 0.93f, 0.96f);

            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                mainCamera.gameObject.AddComponent<AudioListener>();
        }

        private void BuildSceneBindings()
        {
            circleSprite = CreateCircleSprite(256, Color.white);
            spotlightGlowSprite = CreateRadialSprite(256, new Color(1f, 0.82f, 0.28f, 0.34f), 0.62f);

            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            EnsureSerializedSceneReferences();

            foreach (var renderer in lineArtRenderers)
            {
                if (renderer != null && renderer.sprite == null && config.lineArtMap != null)
                    renderer.sprite = config.lineArtMap;
            }

            foreach (var renderer in colorMapRenderers)
            {
                if (renderer != null && renderer.sprite == null && config.colorMap != null)
                    renderer.sprite = config.colorMap;
            }

            ConfigureSceneMaskInteractions();
            BuildSpotlight();

            mapLocalBounds = CalculateMapLocalBounds();
            if (mapLocalBounds.size.sqrMagnitude <= 0.001f)
                mapLocalBounds = new Bounds(Vector3.zero, new Vector3(8f, 12f, 0f));

            cameraRig = new MapCameraRig(mainCamera, transform, config, mapLocalBounds);
            BindSpineViews();
            BindSceneTargets();
            ResetMapAndSpotlight();
        }

        private void BindSpineViews()
        {
            spineViews.Clear();
            EnsureSerializedSceneReferences();

            foreach (var view in spotlightSpineViews)
            {
                if (view == null)
                    continue;

                view.Bind(config);
                if (!spineViews.Contains(view))
                    spineViews.Add(view);
            }

            foreach (var skeleton in spotlightVisibleSkeletonRenderers)
            {
                if (skeleton == null)
                    continue;

                var view = skeleton.GetComponent<SpotlightSpineView>() ?? skeleton.gameObject.AddComponent<SpotlightSpineView>();
                view.Bind(config);

                if (!spineViews.Contains(view))
                    spineViews.Add(view);
            }
        }

        private void BuildSpotlight()
        {
            if (spotlightMask == null)
                spotlightMask = UnityEngine.Object.FindFirstObjectByType<SpriteMask>(FindObjectsInactive.Include);

            if (spotlightMask == null || !spotlightMask.name.Contains("Spotlight", StringComparison.OrdinalIgnoreCase))
            {
                var maskGo = new GameObject("SpotlightMask");
                maskGo.transform.SetParent(transform.parent != null ? transform.parent : transform, false);
                spotlightMask = maskGo.AddComponent<SpriteMask>();
            }

            spotlightMask.sprite = circleSprite;
            spotlightMask.frontSortingOrder = 25;
            spotlightMask.backSortingOrder = -5;

            if (spotlightVisual == null)
            {
                var visual = GameObject.Find("SpotlightVisual");
                spotlightVisual = visual != null
                    ? visual.transform
                    : CreateWorldSprite("SpotlightVisual", transform.parent != null ? transform.parent : transform, spotlightGlowSprite, 30).transform;
            }

            var visualRenderer = spotlightVisual.GetComponent<SpriteRenderer>() ?? spotlightVisual.gameObject.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = spotlightGlowSprite;
            visualRenderer.sortingOrder = Mathf.Max(visualRenderer.sortingOrder, 30);
            visualRenderer.color = Color.white;
        }

        private void BindSceneTargets()
        {
            targets.Clear();
            EnsureSerializedSceneReferences();
            var sceneTargets = targetViews;

            for (int i = 0; i < sceneTargets.Count; i++)
            {
                var view = sceneTargets[i];
                if (view == null)
                    continue;

                var targetConfig = ResolveTargetConfig(view, i);
                if (targetConfig == null)
                    continue;

                view.Bind(targetConfig, config);
                targets.Add(view);
            }

            if (targets.Count == 0)
                Debug.LogWarning("Map: Map 下没有找到可绑定的 HiddenObjectTargetView，请把组件挂到已摆放的找物/Spine 对象上。", this);
        }

        private void EnsureSerializedSceneReferences()
        {
            if (lineArtRenderers.Count == 0)
                AddIfNotNull(lineArtRenderers, FindSpriteRendererInMap("LineArt_xiangao"));

            if (colorMapRenderers.Count == 0)
                AddIfNotNull(colorMapRenderers, FindSpriteRendererInMap("ColorMap_shangse"));

            if (spotlightVisibleSpriteRenderers.Count == 0)
            {
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer == null || IsInList(lineArtRenderers, renderer) || IsInList(colorMapRenderers, renderer))
                        continue;

                    spotlightVisibleSpriteRenderers.Add(renderer);
                }
            }

            foreach (var skeleton in GetComponentsInChildren<SkeletonRenderer>(true))
                AddIfNotNull(spotlightVisibleSkeletonRenderers, skeleton);

            foreach (var view in GetComponentsInChildren<SpotlightSpineView>(true))
                AddIfNotNull(spotlightSpineViews, view);

            if (targetViews.Count == 0)
                targetViews.AddRange(GetComponentsInChildren<HiddenObjectTargetView>(true));
        }

        private SpriteRenderer FindSpriteRendererInMap(string objectName)
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                    return renderer;
            }

            return null;
        }

        private void ConfigureSceneMaskInteractions()
        {
            foreach (var renderer in lineArtRenderers)
                if (renderer != null)
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

            foreach (var renderer in colorMapRenderers)
                if (renderer != null)
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            foreach (var renderer in spotlightVisibleSpriteRenderers)
                if (renderer != null)
                    renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            foreach (var skeleton in spotlightVisibleSkeletonRenderers)
                if (skeleton != null)
                    skeleton.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        private Bounds CalculateMapLocalBounds()
        {
            bool hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (renderer.GetComponentInParent<HiddenObjectTargetView>() != null)
                    continue;

                var localBounds = WorldBoundsToLocal(renderer.bounds, transform);
                if (!hasBounds)
                {
                    bounds = localBounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localBounds.min);
                    bounds.Encapsulate(localBounds.max);
                }
            }

            return hasBounds ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        private Bounds WorldBoundsToLocal(Bounds worldBounds, Transform localRoot)
        {
            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    var world = new Vector3(
                        x == 0 ? worldBounds.min.x : worldBounds.max.x,
                        y == 0 ? worldBounds.min.y : worldBounds.max.y,
                        0f);
                    var local = localRoot.InverseTransformPoint(world);
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                }
            }

            var result = new Bounds((min + max) * 0.5f, max - min);
            result.center = new Vector3(result.center.x, result.center.y, 0f);
            result.size = new Vector3(result.size.x, result.size.y, 0f);
            return result;
        }

        private void AddIfNotNull<T>(List<T> list, T item) where T : UnityEngine.Object
        {
            if (item != null && !list.Contains(item))
                list.Add(item);
        }

        private bool IsInList<T>(List<T> list, T item) where T : UnityEngine.Object
        {
            return item != null && list.Contains(item);
        }

        private HiddenObjectTargetConfig ResolveTargetConfig(HiddenObjectTargetView view, int index)
        {
            if (view == null || config.targets == null || config.targets.Count == 0)
                return null;

            string objectName = view.name;
            foreach (var targetConfig in config.targets)
            {
                if (targetConfig == null || string.IsNullOrWhiteSpace(targetConfig.id))
                    continue;

                if (objectName.Equals(targetConfig.id, StringComparison.OrdinalIgnoreCase) ||
                    objectName.Equals("Target_" + targetConfig.id, StringComparison.OrdinalIgnoreCase) ||
                    objectName.Contains(targetConfig.id, StringComparison.OrdinalIgnoreCase))
                    return targetConfig;
            }

            return index >= 0 && index < config.targets.Count ? config.targets[index] : null;
        }

        private void ResetGameplayCancellation()
        {
            gameplayCts?.Cancel();
            gameplayCts?.Dispose();
            gameplayCts = new CancellationTokenSource();
        }

        private void CancelAndDisposeGameplayCts()
        {
            gameplayCts?.Cancel();
            gameplayCts?.Dispose();
            gameplayCts = null;
        }

        private void ResetMapAndSpotlight()
        {
            cameraRig?.Reset();
            UpdateSpotlightRadius();

            var center = transform.TransformPoint(mapLocalBounds.center);
            if (spotlightMask != null)
                spotlightMask.transform.position = center;

            if (spotlightVisual != null)
                spotlightVisual.position = center;
        }

        private void UpdateSpotlightRadius()
        {
            if (cameraRig == null || spotlightMask == null || spotlightVisual == null) return;

            float radiusPixels = Mathf.Min(Screen.width, Screen.height) * config.spotlightRadiusScreenRatio;
            spotlightRadiusWorld = cameraRig.ScreenToWorldDelta(new Vector2(radiusPixels, 0f)).magnitude;
            float diameter = spotlightRadiusWorld * 2f;
            spotlightMask.transform.localScale = Vector3.one * diameter;
            spotlightVisual.localScale = Vector3.one * diameter;
        }

        private void UpdateTargetsLighting()
        {
            foreach (var spineView in spineViews)
            {
                bool lit = spotlightEnabled && spotlightMask != null &&
                           spineView.IsLitBySpotlight(spotlightMask.transform.position, spotlightRadiusWorld);
                spineView.SetSpotlightState(lit);
            }

            foreach (var target in targets)
            {
                bool lit = spotlightEnabled && spotlightMask != null &&
                            Vector2.Distance(target.WorldPosition, spotlightMask.transform.position) <= spotlightRadiusWorld;

                target.SetSpotlightState(lit);
            }
        }

        private void HandleInput(float dt)
        {
            var touches = GetActiveTouches();
            if (touches.Count == 2)
            {
                float distance = Vector2.Distance(touches[0].position, touches[1].position);
                if (!lastTouchWasPinch)
                {
                    lastTouchWasPinch = true;
                    lastPinchDistance = distance;
                }
                else
                {
                    cameraRig.ZoomBy((distance - lastPinchDistance) * config.pinchZoomSpeed);
                    lastPinchDistance = distance;
                }
                return;
            }

            lastTouchWasPinch = false;

            if (touches.Count == 1)
            {
                var touch = touches[0];
                if (touch.began && IsPointerOverUi(touch.touchId)) return;

                HandlePointer(touch.position, touch.delta, touch.began, touch.ended, dt);
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null) return;

            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
                cameraRig.ZoomBy(wheel * config.wheelZoomSpeed * 0.01f);

            Vector2 mousePosition = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (IsPointerOverUi(-1)) return;
                HandlePointer(mousePosition, Vector2.zero, true, false, dt);
            }
            else if (mouse.leftButton.isPressed)
            {
                HandlePointer(mousePosition, mouse.delta.ReadValue(), false, false, dt);
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                HandlePointer(mousePosition, Vector2.zero, false, true, dt);
            }
        }

        private List<PointerTouch> GetActiveTouches()
        {
            var result = new List<PointerTouch>(2);
            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return result;

            foreach (var touch in touchscreen.touches)
            {
                bool active = touch.press.isPressed || touch.press.wasReleasedThisFrame;
                if (!active) continue;

                result.Add(new PointerTouch
                {
                    touchId = touch.touchId.ReadValue(),
                    position = touch.position.ReadValue(),
                    delta = touch.delta.ReadValue(),
                    began = touch.press.wasPressedThisFrame,
                    ended = touch.press.wasReleasedThisFrame
                });

                if (result.Count >= 2) break;
            }

            return result;
        }

        private void HandlePointer(Vector2 screenPosition, Vector2 screenDelta, bool began, bool ended, float dt)
        {
            var world = cameraRig.ScreenToWorldPoint(screenPosition);

            if (began)
            {
                pointerDown = true;
                pointerMoved = false;
                pointerDownScreen = screenPosition;
                draggingSpotlight = false;
                draggingMap = false;
            }

            if (!began && !ended)
            {
                if (pointerDown && !pointerMoved && Vector2.Distance(pointerDownScreen, screenPosition) > 8f)
                {
                    pointerMoved = true;
                    draggingSpotlight = spotlightEnabled && spotlightMask != null &&
                                        Vector2.Distance(world, spotlightMask.transform.position) <= spotlightRadiusWorld * 1.15f;
                    draggingMap = !draggingSpotlight;
                }

                if (draggingSpotlight)
                {
                    var desired = spotlightMask.transform.position + cameraRig.ScreenToWorldDelta(screenDelta) * config.spotlightDragSpeed;
                    var next = cameraRig.ClampSpotlightWorldPosition(desired, spotlightRadiusWorld);
                    if ((desired - next).sqrMagnitude > 0.0001f)
                        ShowEdgeLimitHint();

                    spotlightMask.transform.position = next;
                    spotlightVisual.position = next;
                    cameraRig.FollowSpotlightNearScreenEdge(next, dt);
                }
                else if (draggingMap)
                {
                    cameraRig.DragByScreenDelta(screenDelta);
                    if (cameraRig.IsMapBeyondBounds(0.02f))
                        ShowEdgeLimitHint();
                }
            }

            if (ended)
            {
                if (!pointerMoved)
                    TryClickTarget(world).Forget();

                pointerDown = false;
                pointerMoved = false;
                draggingMap = false;
                draggingSpotlight = false;
            }
        }

        private void ShowEdgeLimitHint()
        {
            if (Time.unscaledTime < nextEdgeHintTime)
                return;

            nextEdgeHintTime = Time.unscaledTime + 0.35f;
            mainPanel?.ShowEdgeLimitHint();
        }

        private async UniTask TryClickTarget(Vector3 world)
        {
            try
            {
                foreach (var spineView in spineViews)
                {
                    if (!spineView.ContainsWorldPoint(world))
                        continue;

                    bool interacted = await spineView.TryInteract(PlaySpineInteractSound);
                    return;
                }

                foreach (var target in targets)
                {
                    if (!target.ContainsWorldPoint(world)) continue;

                    if (!TryResolveTargetItemId(target, out _))
                    {
                        await PlayWrongFeedback();
                        return;
                    }

                    bool interacted = await target.TryInteract(PlayTargetInteractSound);
                    if (!interacted)
                        return;

                    await CollectTarget(target);
                    return;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask CollectTarget(HiddenObjectTargetView target)
        {
            var token = gameplayCts != null ? gameplayCts.Token : CancellationToken.None;
            token.ThrowIfCancellationRequested();

            Handheld.Vibrate();

            Vector2 flyEnd = ResolveCollectFlyEnd(target);
            if (mainPanel != null && TryResolveTargetItemId(target, out int itemId))
            {
                bool prepared = mainPanel.PrepareItemFound(itemId, out _);
                if (prepared)
                {
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
                    UnityEngine.Canvas.ForceUpdateCanvases();

                    if (mainPanel.TryGetItemWorldPosition(itemId, out var itemWorldPosition))
                        flyEnd = WorldToCanvasLocalPosition(itemWorldPosition);
                }
            }

            await FlyTargetToFindBar(target, flyEnd, token);
            token.ThrowIfCancellationRequested();
            target.MarkCompleted();

            if (context != null && context.TryGet(out MatchSystem match))
                match.Hit();
        }

        private async UniTask FlyTargetToFindBar(HiddenObjectTargetView target, Vector2 end, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (canvas == null) return;

            var flyGo = new GameObject("CollectFly_" + target.Id);
            flyGo.transform.SetParent(canvas.transform, false);
            var image = flyGo.AddComponent<Image>();
            image.sprite = target.Config.iconSprite != null ? target.Config.iconSprite : target.Config.worldSprite;
            image.preserveAspect = true;

            var rt = image.rectTransform;
            rt.sizeDelta = new Vector2(128f, 128f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, mainCamera.WorldToScreenPoint(target.WorldPosition), null, out var start);

            float elapsed = 0f;
            try
            {
                while (elapsed < config.collectFlyDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / config.collectFlyDuration);
                    float eased = 1f - Mathf.Pow(1f - t, 3f);
                    var arc = Vector2.up * Mathf.Sin(t * Mathf.PI) * 160f;
                    rt.anchoredPosition = Vector2.Lerp(start, end, eased) + arc;
                    rt.localScale = Vector3.one * Mathf.Lerp(config.collectPopScale, 0.55f, eased);
                    await UniTask.Yield(token);
                }
            }
            finally
            {
                if (flyGo != null)
                    Destroy(flyGo);
            }
        }

        private Vector2 ResolveCollectFlyEnd(HiddenObjectTargetView target)
        {
            if (mainPanel != null && TryResolveTargetItemId(target, out int itemId) &&
                mainPanel.TryGetItemWorldPosition(itemId, out var itemWorldPosition))
            {
                return WorldToCanvasLocalPosition(itemWorldPosition);
            }

            return canvas != null
                ? new Vector2(0f, -((RectTransform)canvas.transform).rect.height * 0.42f)
                : Vector2.zero;
        }

        private bool TryResolveTargetItemId(HiddenObjectTargetView target, out int itemId)
        {
            itemId = 0;

            if (target == null || context == null || !context.TryGet(out DataManager dataManager))
                return false;

            var levelData = dataManager.CurrentLevelData;
            if (levelData == null)
                return false;

            if (int.TryParse(target.Id, out itemId) && levelData.ContainsTarget(itemId))
                return true;

            string displayName = target.Config != null ? target.Config.displayName : null;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                foreach (var item in levelData.Items)
                {
                    if (item == null || item.ItemName != displayName)
                        continue;

                    itemId = item.ItemId;
                    return levelData.ContainsTarget(itemId);
                }
            }

            return false;
        }

        private Vector2 WorldToCanvasLocalPosition(Vector3 worldPosition)
        {
            if (canvas == null)
                return Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform,
                RectTransformUtility.WorldToScreenPoint(null, worldPosition),
                null,
                out var localPosition);

            return localPosition;
        }

        private async UniTask PlayWrongFeedback()
        {
            if (config.defaultWrongClip != null)
            {
                if (sfxSource == null)
                    sfxSource = gameObject.AddComponent<AudioSource>();

                sfxSource.enabled = true;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.mute = false;
                sfxSource.PlayOneShot(config.defaultWrongClip);
            }

            if (mainCamera == null || config.wrongShakeDuration <= 0f || config.wrongShakeDistance <= 0f)
                return;

            var token = gameplayCts != null ? gameplayCts.Token : CancellationToken.None;
            Vector3 origin = mainCamera.transform.position;
            float elapsed = 0f;
            float shakeWorld = cameraRig != null
                ? cameraRig.ScreenToWorldDelta(new Vector2(config.wrongShakeDistance, 0f)).magnitude
                : config.wrongShakeDistance * 0.01f;

            try
            {
                while (elapsed < config.wrongShakeDuration)
                {
                    token.ThrowIfCancellationRequested();
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / config.wrongShakeDuration);
                    float strength = (1f - t) * shakeWorld;
                    float phase = elapsed * 90f;
                    var offset = new Vector3(Mathf.Sin(phase), Mathf.Cos(phase * 1.37f), 0f) * strength;
                    mainCamera.transform.position = origin + offset;
                    await UniTask.Yield(token);
                }
            }
            finally
            {
                if (mainCamera != null)
                    mainCamera.transform.position = origin;
            }
        }

        private void PlayLevelBgm()
        {
            var clip = ResolveLevelBgm();
            if (clip == null)
            {
                if (logBgmState)
                    Debug.LogWarning("[Map] 没有找到可播放的 BGM。请检查当前 LevelData 的 bgm 字段是否已赋值，或 HiddenObjectDemoConfig.backgroundClip 是否有兜底音频。", this);
                return;
            }

            if (bgmSource == null)
                bgmSource = gameObject.AddComponent<AudioSource>();

            AudioListener.pause = false;
            AudioListener.volume = 1f;

            bgmSource.enabled = true;
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f;
            bgmSource.volume = bgmVolume;
            bgmSource.mute = false;
            bgmSource.bypassEffects = false;
            bgmSource.bypassListenerEffects = false;
            bgmSource.bypassReverbZones = false;
            bgmSource.ignoreListenerPause = false;

            if (bgmSource.clip == clip && bgmSource.isPlaying)
                return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        private void StopLevelBgm()
        {
            if (bgmSource == null)
                return;

            bgmSource.Stop();
        }

        private AudioClip ResolveLevelBgm()
        {
            if (context != null && context.TryGet(out DataManager dataManager))
            {
                var levelBgm = dataManager.CurrentLevelData?.Bgm;
                if (levelBgm != null)
                    return levelBgm;
            }

            return config.backgroundClip;
        }

        private void PlaySpineInteractSound(SpotlightSpineView spineView)
        {
            var clip = spineView != null ? spineView.InteractClip : null;
            if (clip == null)
                return;

            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.enabled = true;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.mute = false;
            sfxSource.PlayOneShot(clip);
        }

        private void PlayTargetInteractSound(HiddenObjectTargetView target)
        {
            var clip = target != null ? target.InteractClip : null;
            if (clip == null)
                return;

            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.enabled = true;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.mute = false;
            sfxSource.PlayOneShot(clip);
        }

        private bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null) return false;
            return pointerId >= 0 ? EventSystem.current.IsPointerOverGameObject(pointerId) : EventSystem.current.IsPointerOverGameObject();
        }

        private SpriteRenderer CreateWorldSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private Sprite CreateCircleSprite(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private Sprite CreateRadialSprite(int size, Color color, float solidRadius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float normalized = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = 1f - Mathf.SmoothStep(solidRadius, 1f, normalized);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private struct PointerTouch
        {
            public int touchId;
            public Vector2 position;
            public Vector2 delta;
            public bool began;
            public bool ended;
        }

        private void LoadEditorDefaults(HiddenObjectDemoConfig target)
        {
#if UNITY_EDITOR
            target.lineArtMap ??= LoadSpriteAtPath("Assets/ZYQ/Sprites/大图/xiangao.png", "xiangao_0");
            target.colorMap ??= LoadSpriteAtPath("Assets/ZYQ/Sprites/大图/shangse.png", "shangse_0");

            if (target.targets.Count > 0) return;

            target.targets.Add(CreateTarget("huluobo", "胡萝卜", "huluobo", "ui_huluobo", new Vector2(0.24f, 0.34f), "chushi/chushi_SkeletonData.asset"));
            target.targets.Add(CreateTarget("yugutou", "鱼骨头", "yugutou", "ui_yugutou", new Vector2(0.68f, 0.38f), "fuwuyuan/fuwuyuan_SkeletonData.asset"));
            target.targets.Add(CreateTarget("yumao", "羽毛", "yumao", "ui_yumao", new Vector2(0.42f, 0.58f), "xiaonvhai/xiaonvhai_SkeletonData.asset"));
            target.targets.Add(CreateTarget("rouchuan", "肉串", "rouchuan", "ui_rouchuan", new Vector2(0.78f, 0.66f), "huo1/huo_SkeletonData.asset"));
            target.targets.Add(CreateTarget("yaoshi", "钥匙", "yaoshi", "ui_yaoshi", new Vector2(0.31f, 0.74f), "e/e_SkeletonData.asset"));
            target.targets.Add(CreateTarget("tiaoliao", "调料", "ui_tiaoliao", "ui_tiaoliao", new Vector2(0.56f, 0.25f), null));
#endif
        }

#if UNITY_EDITOR
        private HiddenObjectTargetConfig CreateTarget(string id, string displayName, string worldSpriteName, string iconSpriteName, Vector2 position, string skeletonPath)
        {
            return new HiddenObjectTargetConfig
            {
                id = id,
                displayName = displayName,
                worldSprite = LoadSpriteAtPath($"Assets/ZYQ/Sprites/目标物/{worldSpriteName}.png", worldSpriteName + "_0"),
                iconSprite = LoadSpriteAtPath($"Assets/ZYQ/Sprites/目标物/{iconSpriteName}.png", iconSpriteName + "_0"),
                normalizedPosition = position,
                worldSize = new Vector2(0.75f, 0.75f),
                hasInteractAnimation = skeletonPath != null,
                skeletonData = string.IsNullOrEmpty(skeletonPath) ? null : AssetDatabase.LoadAssetAtPath<Spine.Unity.SkeletonDataAsset>($"Assets/ZYQ/Spine/{skeletonPath}")
            };
        }

        private Sprite LoadSpriteAtPath(string path, string preferredName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite candidate && (string.IsNullOrEmpty(preferredName) || candidate.name == preferredName))
                    return candidate;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite candidate)
                    return candidate;
            }

            return null;
        }
#endif
    }
}
