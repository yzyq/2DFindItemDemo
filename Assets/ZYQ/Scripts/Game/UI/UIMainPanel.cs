using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZYQ.Demo
{
    public class UIMainPanel : UIPanel
    {
        [Header("Buttons")]
        [SerializeField] private Button returnBtn;
        [SerializeField] private Button resetBtn;
        [SerializeField] private Button spotlightBtn;
        [SerializeField] private Button otherBtn;
        [SerializeField] private Button collapseBtn;

        [SerializeField] private Sprite spotlightOn;
        [SerializeField] private Sprite spotlightOff;

        [Header("Audio")]
        [SerializeField] private AudioSource uiAudioSource;
        [SerializeField] private AudioClip buttonClickClip;

        [Header("Find View")]
        [SerializeField] private RectTransform findViewRect;

        [Header("Find View Collapse Animation")]
        [SerializeField] private float collapseDuration = 0.25f;

        [Tooltip("收起方向。-1 表示向下收起，1 表示向上收起。方向反了就改这个值。")]
        [SerializeField] private float collapseDirectionY = -1f;

        [Tooltip("在 findViewRect 高度基础上额外移动的距离，避免边缘露出来。")]
        [SerializeField] private float collapseExtraOffset = 20f;

        [Tooltip("如果 findViewRect 高度为 0，则使用这个备用收起距离。")]
        [SerializeField] private float fallbackCollapseDistance = 160f;

        [SerializeField] private bool resetFindViewOnShow = true;
        [SerializeField] private Ease collapseEase = Ease.OutCubic;

        [Header("Scroll View")]
        [SerializeField] private LevelItemsScrollView levelItemsScrollView;

        [Header("Edge Hint")]
        [SerializeField] private TMP_Text edgeHintText;
        [SerializeField] private string edgeHintMessage = "已经到边缘了";
        [SerializeField] private float edgeHintFadeInDuration = 0.12f;
        [SerializeField] private float edgeHintStayDuration = 0.55f;
        [SerializeField] private float edgeHintFadeOutDuration = 0.22f;

        [Header("Complete Popup")]
        [SerializeField] private GameObject completePopup;
        [SerializeField] private TMP_Text completeTipText;
        [SerializeField] private string completeTipMessage = "恭喜，当前关卡目标已全部找到！";
        [SerializeField] private Button completeNextBtn;
        [SerializeField] private Button completeRestartBtn;

        private IReadOnlyList<LevelItemData> targetItemCache = Array.Empty<LevelItemData>();
        private readonly HashSet<int> foundItemIds = new HashSet<int>();

        private LevelDataConfig currentLevel;
        private LevelData currentLevelData;

        private RectTransform collapseBtnRect;
        private Vector2 findViewExpandedPos;
        private Vector2 findViewCollapsedPos;
        private Vector2 collapseBtnExpandedPos;
        private Vector2 collapseBtnCollapsedPos;
        private Quaternion collapseBtnExpandedRotation;
        private Quaternion collapseBtnCollapsedRotation;

        private bool findViewCollapsed;
        private bool findViewStateCached;
        private bool completePopupShown;

        private CancellationTokenSource collapseCts;
        private CancellationTokenSource edgeHintCts;

        public event Action ReturnClicked;
        public event Action ResetClicked;
        public event Action SpotlightClicked;
        public event Action OtherClicked;

        private void OnValidate()
        {
            Type = UIPanelType.Game;
        }

        protected override void OnShow()
        {
            base.OnShow();

            Debug.Log("UIMainPanel Show");

            CacheFindViewStateIfNeeded();

            if (resetFindViewOnShow)
            {
                ResetFindViewState();
            }

            BindButtons();
            BindCompletePopupButtons();
            LoadCurrentLevelFromDataManager();
            HideEdgeHintImmediate();
            HideCompletePopup();
        }

        protected override void OnHide()
        {
            CancelCollapseAnimation();
            CancelEdgeHint();
            UnbindButtons();
            UnbindCompletePopupButtons();
            HideCompletePopup();

            base.OnHide();

            Debug.Log("UIMainPanel Hide");
        }

        private void BindButtons()
        {
            if (returnBtn != null)
            {
                returnBtn.onClick.RemoveListener(HandleReturnClicked);
                returnBtn.onClick.AddListener(HandleReturnClicked);
            }

            if (resetBtn != null)
            {
                resetBtn.onClick.RemoveListener(HandleResetClicked);
                resetBtn.onClick.AddListener(HandleResetClicked);
            }

            if (spotlightBtn != null)
            {
                spotlightBtn.onClick.RemoveListener(HandleSpotlightClicked);
                spotlightBtn.onClick.AddListener(HandleSpotlightClicked);
            }

            if (otherBtn != null)
            {
                otherBtn.onClick.RemoveListener(HandleOtherClicked);
                otherBtn.onClick.AddListener(HandleOtherClicked);
            }

            if (collapseBtn != null)
            {
                collapseBtn.onClick.RemoveListener(HandleCollapseClicked);
                collapseBtn.onClick.AddListener(HandleCollapseClicked);
            }
        }

        private void BindCompletePopupButtons()
        {
            if (completeNextBtn != null)
            {
                completeNextBtn.onClick.RemoveListener(HandleCompleteNextClicked);
                completeNextBtn.onClick.AddListener(HandleCompleteNextClicked);
            }

            if (completeRestartBtn != null)
            {
                completeRestartBtn.onClick.RemoveListener(HandleCompleteRestartClicked);
                completeRestartBtn.onClick.AddListener(HandleCompleteRestartClicked);
            }
        }

        private void UnbindButtons()
        {
            if (returnBtn != null)
            {
                returnBtn.onClick.RemoveListener(HandleReturnClicked);
            }

            if (resetBtn != null)
            {
                resetBtn.onClick.RemoveListener(HandleResetClicked);
            }

            if (spotlightBtn != null)
            {
                spotlightBtn.onClick.RemoveListener(HandleSpotlightClicked);
            }

            if (otherBtn != null)
            {
                otherBtn.onClick.RemoveListener(HandleOtherClicked);
            }

            if (collapseBtn != null)
            {
                collapseBtn.onClick.RemoveListener(HandleCollapseClicked);
            }
        }

        private void UnbindCompletePopupButtons()
        {
            if (completeNextBtn != null)
            {
                completeNextBtn.onClick.RemoveListener(HandleCompleteNextClicked);
            }

            if (completeRestartBtn != null)
            {
                completeRestartBtn.onClick.RemoveListener(HandleCompleteRestartClicked);
            }
        }

        private void CacheFindViewStateIfNeeded()
        {
            if (findViewStateCached) return;

            if (findViewRect == null)
            {
                Debug.LogWarning("UIMainPanel 缺少 findViewRect，收起展开动画不可用。", this);
                return;
            }

            collapseBtnRect = collapseBtn != null
                ? collapseBtn.transform as RectTransform
                : null;

            findViewExpandedPos = findViewRect.anchoredPosition;

            float rectHeight = findViewRect.rect.height;
            float distance = rectHeight > 0f
                ? rectHeight + collapseExtraOffset
                : fallbackCollapseDistance;

            float direction = Mathf.Approximately(collapseDirectionY, 0f)
                ? -1f
                : Mathf.Sign(collapseDirectionY);

            findViewCollapsedPos = findViewExpandedPos + new Vector2(0f, distance * direction);

            if (collapseBtnRect != null)
            {
                collapseBtnExpandedPos = collapseBtnRect.anchoredPosition;
                collapseBtnCollapsedPos = collapseBtnExpandedPos + new Vector2(0f, distance * direction);
                collapseBtnExpandedRotation = collapseBtnRect.localRotation;
                collapseBtnCollapsedRotation = collapseBtnExpandedRotation * Quaternion.Euler(0f, 0f, 180f);
            }

            findViewStateCached = true;
        }

        private void ResetFindViewState()
        {
            CancelCollapseAnimation();

            findViewCollapsed = false;

            if (findViewRect != null)
            {
                findViewRect.anchoredPosition = findViewExpandedPos;
            }

            if (collapseBtnRect != null)
            {
                collapseBtnRect.anchoredPosition = collapseBtnExpandedPos;
                collapseBtnRect.localRotation = collapseBtnExpandedRotation;
            }
        }

        private void HandleCollapseClicked()
        {
            PlayButtonClickSound();
            ToggleFindViewAsync().Forget();
        }

        private async UniTaskVoid ToggleFindViewAsync()
        {
            if (findViewRect == null) return;

            CacheFindViewStateIfNeeded();

            CancelCollapseAnimation();

            collapseCts = new CancellationTokenSource();
            CancellationToken token = collapseCts.Token;

            bool targetCollapsed = !findViewCollapsed;

            Vector2 startPos = findViewRect.anchoredPosition;
            Vector2 targetPos = targetCollapsed
                ? findViewCollapsedPos
                : findViewExpandedPos;

            Quaternion startRotation = collapseBtnRect != null
                ? collapseBtnRect.localRotation
                : Quaternion.identity;

            Quaternion targetRotation = targetCollapsed
                ? collapseBtnCollapsedRotation
                : collapseBtnExpandedRotation;

            Vector2 startButtonPos = collapseBtnRect != null
                ? collapseBtnRect.anchoredPosition
                : Vector2.zero;

            Vector2 targetButtonPos = targetCollapsed
                ? collapseBtnCollapsedPos
                : collapseBtnExpandedPos;

            try
            {
                if (collapseBtnRect != null)
                {
                    MotionHandle findViewMotion = LMotion.Create(startPos, targetPos, collapseDuration)
                        .WithEase(collapseEase)
                        .Bind(value => findViewRect.anchoredPosition = value);

                    MotionHandle collapseButtonMotion = LMotion.Create(0f, 1f, collapseDuration)
                        .WithEase(collapseEase)
                        .Bind(value =>
                        {
                            collapseBtnRect.anchoredPosition = Vector2.Lerp(
                                startButtonPos,
                                targetButtonPos,
                                value);

                            collapseBtnRect.localRotation = Quaternion.Lerp(
                                startRotation,
                                targetRotation,
                                value);
                        });

                    await UniTask.WhenAll(
                        WaitMotionAsync(findViewMotion, token),
                        WaitMotionAsync(collapseButtonMotion, token)
                    );
                }
                else
                {
                    MotionHandle findViewMotion = LMotion.Create(startPos, targetPos, collapseDuration)
                        .WithEase(collapseEase)
                        .Bind(value => findViewRect.anchoredPosition = value);

                    await WaitMotionAsync(findViewMotion, token);
                }

                findViewCollapsed = targetCollapsed;
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void CancelCollapseAnimation()
        {
            if (collapseCts == null) return;

            collapseCts.Cancel();
            collapseCts.Dispose();
            collapseCts = null;
        }

        private void LoadCurrentLevelFromDataManager()
        {
            if (context == null || !context.TryGet(out DataManager dataManager))
            {
                Debug.LogError("DataManager 未注册。", this);
                return;
            }

            currentLevel = dataManager.CurrentLevel;
            currentLevelData = dataManager.CurrentLevelData;

            if (currentLevel == null)
            {
                Debug.LogError("DataManager 中没有当前关卡数据。", this);
                return;
            }

            LoadLevel(currentLevelData);
        }

        private void LoadLevel(LevelData levelData)
        {
            currentLevelData = levelData;

            foundItemIds.Clear();
            completePopupShown = false;
            targetItemCache = levelData != null ? levelData.Items : Array.Empty<LevelItemData>();
            HideCompletePopup();

            if (levelItemsScrollView != null)
            {
                levelItemsScrollView.SetData(targetItemCache);
                levelItemsScrollView.ClearFoundItems();
            }
            else
            {
                Debug.LogError("UIMainPanel 缺少 LevelItemsScrollView。", this);
            }
        }


        /// <summary>
        /// 游戏过程中找到某个物品时调用。
        /// </summary>
        public void MarkItemFound(int itemId)
        {
            if (currentLevel == null)
            {
                Debug.LogWarning("当前没有加载关卡，无法标记找到物品。", this);
                return;
            }

            if (currentLevelData == null || !currentLevelData.ContainsTarget(itemId))
            {
                Debug.LogWarning($"物品 {itemId} 不是当前关卡需要查找的目标。", this);
                return;
            }

            if (!foundItemIds.Add(itemId))
            {
                return;
            }

            if (levelItemsScrollView != null)
            {
                levelItemsScrollView.MarkItemFound(itemId);
            }

            CheckLevelComplete();
        }

        public void MarkTargetFoundByIndex(int targetIndex)
        {
            if (!TryGetTargetItemId(targetIndex, out int itemId))
            {
                return;
            }

            MarkItemFound(itemId);
        }

        public bool PrepareTargetFoundByIndex(int targetIndex, out Vector3 itemWorldPosition)
        {
            itemWorldPosition = Vector3.zero;

            if (!TryGetTargetItemId(targetIndex, out int itemId))
            {
                return false;
            }

            return PrepareItemFound(itemId, out itemWorldPosition);
        }

        public bool PrepareItemFound(int itemId, out Vector3 itemWorldPosition)
        {
            itemWorldPosition = Vector3.zero;

            if (currentLevelData == null)
            {
                Debug.LogWarning("当前没有 LevelDataSO 关卡数据，无法准备目标收集表现。", this);
                return false;
            }

            if (!currentLevelData.ContainsTarget(itemId))
            {
                Debug.LogWarning($"物品 {itemId} 不是当前关卡需要查找的目标。", this);
                return false;
            }

            foundItemIds.Add(itemId);

            bool resolvedPosition = levelItemsScrollView != null &&
                                    levelItemsScrollView.MoveItemToEndAndMarkFound(itemId, out itemWorldPosition);

            CheckLevelComplete();
            return resolvedPosition;
        }

        public bool TryGetTargetItemWorldPosition(int targetIndex, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (!TryGetTargetItemId(targetIndex, out int itemId))
                return false;

            if (levelItemsScrollView == null)
                return false;

            return levelItemsScrollView.TryGetItemWorldPositionById(itemId, out worldPosition);
        }

        public bool TryGetItemWorldPosition(int itemId, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (levelItemsScrollView == null)
                return false;

            return levelItemsScrollView.TryGetItemWorldPositionById(itemId, out worldPosition);
        }

        private bool TryGetTargetItemId(int targetIndex, out int itemId)
        {
            itemId = 0;

            if (currentLevelData == null)
            {
                Debug.LogWarning("当前没有 LevelDataSO 关卡数据，无法获取目标物品。", this);
                return false;
            }

            var targetIds = currentLevelData.TargetItemIds;
            if (targetIds == null || targetIndex < 0 || targetIndex >= targetIds.Count)
            {
                Debug.LogWarning($"目标下标越界：{targetIndex}", this);
                return false;
            }

            itemId = targetIds[targetIndex];
            return currentLevelData.GetItem(itemId) != null;
        }

        public void ShowEdgeLimitHint()
        {
            ShowEdgeLimitHintAsync().Forget();
        }

        private async UniTaskVoid ShowEdgeLimitHintAsync()
        {
            EnsureEdgeHintText();
            if (edgeHintText == null) return;

            CancelEdgeHint();
            edgeHintCts = new CancellationTokenSource();
            var token = edgeHintCts.Token;

            edgeHintText.text = edgeHintMessage;
            edgeHintText.gameObject.SetActive(true);

            try
            {
                await FadeEdgeHint(edgeHintText.alpha, 1f, edgeHintFadeInDuration, token);
                await UniTask.Delay(TimeSpan.FromSeconds(edgeHintStayDuration), cancellationToken: token);
                await FadeEdgeHint(edgeHintText.alpha, 0f, edgeHintFadeOutDuration, token);
                edgeHintText.gameObject.SetActive(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private UniTask FadeEdgeHint(float from, float to, float duration, CancellationToken token)
        {
            if (duration <= 0f)
            {
                edgeHintText.alpha = to;
                return UniTask.CompletedTask;
            }

            MotionHandle handle = LMotion.Create(from, to, duration)
                .WithEase(Ease.OutCubic)
                .Bind(value => edgeHintText.alpha = value);

            return WaitMotionAsync(handle, token);
        }

        private static async UniTask WaitMotionAsync(MotionHandle handle, CancellationToken token)
        {
            try
            {
                while (handle.IsActive())
                {
                    token.ThrowIfCancellationRequested();
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
                if (handle.IsActive())
                    handle.Cancel();

                throw;
            }
        }

        private void HideEdgeHintImmediate()
        {
            EnsureEdgeHintText();
            if (edgeHintText == null) return;

            edgeHintText.alpha = 0f;
            edgeHintText.gameObject.SetActive(false);
        }

        private void CancelEdgeHint()
        {
            if (edgeHintCts == null) return;

            edgeHintCts.Cancel();
            edgeHintCts.Dispose();
            edgeHintCts = null;
        }

        private void EnsureEdgeHintText()
        {
            if (edgeHintText != null) return;

            var go = new GameObject("EdgeHintText", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.sizeDelta = new Vector2(420f, 72f);

            edgeHintText = go.AddComponent<TextMeshProUGUI>();
            edgeHintText.alignment = TextAlignmentOptions.Center;
            edgeHintText.fontSize = 34f;
            edgeHintText.color = new Color(1f, 1f, 1f, 0f);
            edgeHintText.raycastTarget = false;
        }

        private void CheckLevelComplete()
        {
            if (currentLevel == null) return;

            int targetCount = currentLevelData?.TargetItemIds?.Count ?? 0;
            bool completed = targetCount > 0 && foundItemIds.Count >= targetCount;

            if (completed)
            {
                Debug.Log("当前关卡所有目标物品已找到。");
                ShowCompletePopup();
            }
        }

        private void ShowCompletePopup()
        {
            if (completePopupShown)
                return;

            completePopupShown = true;

            if (completeTipText != null)
                completeTipText.text = completeTipMessage;

            if (completePopup != null)
                completePopup.SetActive(true);
            else
                Debug.LogWarning("UIMainPanel 未绑定 completePopup，无法显示通关提示窗口。", this);
        }

        private void HideCompletePopup()
        {
            if (completePopup != null)
                completePopup.SetActive(false);
        }

        private void HandleReturnClicked()
        {
            PlayButtonClickSound();
            ReturnClicked?.Invoke();
        }

        private void HandleResetClicked()
        {
            PlayButtonClickSound();
            foundItemIds.Clear();
            completePopupShown = false;
            HideCompletePopup();
            LoadCurrentLevelFromDataManager();
            ResetClicked?.Invoke();
        }

        private void HandleCompleteRestartClicked()
        {
            HandleResetClicked();
        }

        private void HandleCompleteNextClicked()
        {
            PlayButtonClickSound();
            HideCompletePopup();
            ReturnClicked?.Invoke();
        }

        private void HandleSpotlightClicked()
        {
            PlayButtonClickSound();
            SpotlightClicked?.Invoke();
            if (spotlightBtn != null)
            {
                bool isOn = spotlightBtn.image.sprite == spotlightOn;
                spotlightBtn.image.sprite = isOn ? spotlightOff : spotlightOn;
            }
        }

        private void HandleOtherClicked()
        {
            PlayButtonClickSound();
            OtherClicked?.Invoke();
        }

        private void PlayButtonClickSound()
        {
            if (buttonClickClip == null)
                return;

            if (uiAudioSource == null)
                uiAudioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            uiAudioSource.enabled = true;
            uiAudioSource.playOnAwake = false;
            uiAudioSource.spatialBlend = 0f;
            uiAudioSource.mute = false;
            uiAudioSource.PlayOneShot(buttonClickClip);
        }
    }
}
