using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZYQ.Demo
{
    public enum PoolScrollDirection
    {
        Vertical,
        Horizontal
    }

    public class PoolScrollView<TData, TItem> : MonoBehaviour
        where TItem : Component, IPoolScrollItem<TData>
    {
        [Header("Scroll")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private PoolScrollDirection direction = PoolScrollDirection.Vertical;

        [Header("Item")]
        [SerializeField] private TItem itemPrefab;
        [SerializeField] private float itemWidth = 160f;
        [SerializeField] private float itemHeight = 160f;
        [SerializeField] private float spacing = 10f;
        [SerializeField] private int bufferCount = 2;

        [Header("Padding")]
        [SerializeField] private float paddingStart = 0f;
        [SerializeField] private float paddingEnd = 0f;

        protected virtual void OnItemDataUpdated(TItem item, TData data, int index)
        {
        }

        private readonly List<TData> dataList = new List<TData>();
        private readonly Dictionary<int, TItem> activeItems = new Dictionary<int, TItem>();
        private readonly List<int> removeCache = new List<int>();

        private UIPool<TItem> pool;

        private float itemStep;
        private bool initialized;

        private void Awake()
        {
            Init();
        }

        private void OnEnable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
            }
        }

        private void OnDisable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            }
        }

        private void OnDestroy()
        {
            ClearActiveItems();
            pool?.Clear();
        }

        private void Init()
        {
            if (initialized) return;

            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
            }

            if (scrollRect == null)
            {
                Debug.LogError($"{nameof(PoolScrollView<TData, TItem>)} 缺少 ScrollRect。", this);
                return;
            }

            if (viewport == null)
            {
                viewport = scrollRect.viewport;
            }

            if (content == null)
            {
                content = scrollRect.content;
            }

            if (viewport == null || content == null || itemPrefab == null)
            {
                Debug.LogError($"{nameof(PoolScrollView<TData, TItem>)} 配置不完整。", this);
                return;
            }

            itemStep = IsVertical ? itemHeight + spacing : itemWidth + spacing;

            pool = new UIPool<TItem>(itemPrefab, content);

            SetupScrollRect();
            SetupContent();

            initialized = true;
        }

        private bool IsVertical => direction == PoolScrollDirection.Vertical;
        protected int DataCount => dataList.Count;

        protected TData GetData(int index)
        {
            return index >= 0 && index < dataList.Count ? dataList[index] : default;
        }

        protected int FindDataIndex(System.Predicate<TData> match)
        {
            if (match == null)
                return -1;

            for (int i = 0; i < dataList.Count; i++)
            {
                if (match(dataList[i]))
                    return i;
            }

            return -1;
        }

        protected bool MoveDataToEnd(int index, bool scrollToEnd = false)
        {
            if (index < 0 || index >= dataList.Count)
                return false;

            if (index == dataList.Count - 1)
            {
                if (scrollToEnd)
                    ScrollToEnd();
                else
                    Refresh();

                return true;
            }

            TData data = dataList[index];
            dataList.RemoveAt(index);
            dataList.Add(data);

            RecalculateContentSize();
            ClearActiveItems();

            if (scrollToEnd)
                ScrollToEnd();
            else
                RefreshVisibleItems(true);

            return true;
        }

        private void SetupScrollRect()
        {
            scrollRect.vertical = IsVertical;
            scrollRect.horizontal = !IsVertical;
            //scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        private void SetupContent()
        {
            if (IsVertical)
            {
                content.anchorMin = new Vector2(0.5f, 1f);
                content.anchorMax = new Vector2(0.5f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
                content.anchoredPosition = Vector2.zero;
            }
            else
            {
                content.anchorMin = new Vector2(0f, 0.5f);
                content.anchorMax = new Vector2(0f, 0.5f);
                content.pivot = new Vector2(0f, 0.5f);
                content.anchoredPosition = Vector2.zero;
            }
        }

        /// <summary>
        /// 设置数据列表并刷新显示。通常在加载新数据或重置列表时调用，参数为新的数据集合和是否重置滚动位置（默认为 true）。      
        /// </summary>
        /// <param name="list"></param>
        /// <param name="resetPosition"></param>
        public void SetData(IReadOnlyList<TData> list, bool resetPosition = true)
        {
            Init();

            dataList.Clear();

            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    dataList.Add(list[i]);
                }
            }

            RecalculateContentSize();
            ClearActiveItems();

            if (resetPosition)
            {
                content.anchoredPosition = Vector2.zero;
            }

            RefreshVisibleItems();
        }

        public void Refresh()
        {
            RefreshVisibleItems(true);
        }

        /// <summary>
        /// 滚动到指定索引位置，确保该项完全可见。通常在外部需要跳转到特定数据项时调用，参数为目标索引。索引会自动限制在有效范围内。
        /// </summary>
        /// <param name="index"></param>
        public void ScrollToIndex(int index)
        {
            if (dataList.Count == 0) return;

            index = Mathf.Clamp(index, 0, dataList.Count - 1);

            float position = paddingStart + index * itemStep;
            position = ClampScrollOffset(position);

            if (IsVertical)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, position);
            }
            else
            {
                content.anchoredPosition = new Vector2(-position, content.anchoredPosition.y);
            }

            RefreshVisibleItems(true);
        }

        public void ScrollToEnd()
        {
            if (dataList.Count == 0) return;

            float position = ClampScrollOffset(GetContentMainSize() - GetViewportMainSize());

            if (IsVertical)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, position);
            }
            else
            {
                content.anchoredPosition = new Vector2(-position, content.anchoredPosition.y);
            }

            RefreshVisibleItems(true);
        }

        public bool TryGetItemWorldPosition(int index, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (!initialized || content == null || index < 0 || index >= dataList.Count)
                return false;

            if (activeItems.TryGetValue(index, out TItem activeItem) && activeItem != null)
            {
                worldPosition = activeItem.transform.position;
                return true;
            }

            var localPosition = IsVertical
                ? new Vector3(0f, -paddingStart - index * itemStep - itemHeight * 0.5f, 0f)
                : new Vector3(paddingStart + index * itemStep + itemWidth * 0.5f, 0f, 0f);

            worldPosition = content.TransformPoint(localPosition);
            return true;
        }

        private void RecalculateContentSize()
        {
            float totalLength = dataList.Count <= 0
                ? 0f
                : paddingStart + paddingEnd + dataList.Count * GetItemMainSize() + Mathf.Max(0, dataList.Count - 1) * spacing;

            if (IsVertical)
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, totalLength);
            }
            else
            {
                content.sizeDelta = new Vector2(totalLength, content.sizeDelta.y);
            }
        }

        private float GetItemMainSize()
        {
            return IsVertical ? itemHeight : itemWidth;
        }

        private float GetContentMainSize()
        {
            return IsVertical ? content.rect.height : content.rect.width;
        }

        private float GetViewportMainSize()
        {
            return IsVertical ? viewport.rect.height : viewport.rect.width;
        }

        private float ClampScrollOffset(float offset)
        {
            return Mathf.Clamp(offset, 0f, Mathf.Max(0f, GetContentMainSize() - GetViewportMainSize()));
        }

        private float GetScrollOffset()
        {
            if (IsVertical)
            {
                return Mathf.Max(0f, content.anchoredPosition.y);
            }

            return Mathf.Max(0f, -content.anchoredPosition.x);
        }

        private void OnScrollChanged(Vector2 value)
        {
            RefreshVisibleItems();
        }

        private void RefreshVisibleItems(bool forceRefresh = false)
        {
            if (!initialized) return;

            if (dataList.Count == 0)
            {
                ClearActiveItems();
                return;
            }

            float scrollOffset = GetScrollOffset();
            float viewSize = GetViewportMainSize();

            int startIndex = Mathf.FloorToInt((scrollOffset - paddingStart) / itemStep) - bufferCount;
            int endIndex = Mathf.CeilToInt((scrollOffset + viewSize - paddingStart) / itemStep) + bufferCount;

            startIndex = Mathf.Clamp(startIndex, 0, dataList.Count - 1);
            endIndex = Mathf.Clamp(endIndex, 0, dataList.Count - 1);

            removeCache.Clear();

            foreach (KeyValuePair<int, TItem> pair in activeItems)
            {
                int index = pair.Key;

                if (index < startIndex || index > endIndex)
                {
                    removeCache.Add(index);
                }
            }

            for (int i = 0; i < removeCache.Count; i++)
            {
                int index = removeCache[i];

                pool.Release(activeItems[index]);
                activeItems.Remove(index);
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                if (activeItems.TryGetValue(i, out TItem activeItem))
                {
                    if (forceRefresh)
                    {
                        activeItem.SetData(dataList[i], i);
                        OnItemDataUpdated(activeItem, dataList[i], i);
                    }

                    UpdateItemPosition(activeItem, i);
                    continue;
                }

                CreateItem(i);
            }
        }

        private void CreateItem(int index)
        {
            TItem item = pool.Get();

            RectTransform rect = item.transform as RectTransform;

            if (rect == null)
            {
                Debug.LogError("Item 必须是 UI 对象，并且包含 RectTransform。", item);
                return;
            }

            SetupItemRect(rect);
            UpdateItemPosition(item, index);

            item.SetData(dataList[index], index);
            OnItemDataUpdated(item, dataList[index], index);
            activeItems.Add(index, item);
        }

        private void SetupItemRect(RectTransform rect)
        {
            if (IsVertical)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
            }

            rect.sizeDelta = new Vector2(itemWidth, itemHeight);
        }

        private void UpdateItemPosition(TItem item, int index)
        {
            RectTransform rect = item.transform as RectTransform;

            if (rect == null) return;

            if (IsVertical)
            {
                float y = -paddingStart - index * itemStep;
                rect.anchoredPosition = new Vector2(0f, y);
            }
            else
            {
                float x = paddingStart + index * itemStep;
                rect.anchoredPosition = new Vector2(x, 0f);
            }
        }

        private void ClearActiveItems()
        {
            foreach (KeyValuePair<int, TItem> pair in activeItems)
            {
                pool.Release(pair.Value);
            }

            activeItems.Clear();
            removeCache.Clear();
        }
    }
}
