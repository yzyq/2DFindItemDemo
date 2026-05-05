using System.Collections.Generic;

namespace ZYQ.Demo
{
    public class LevelItemsScrollView : PoolScrollView<LevelItemData, LevelItemsItem>
    {
        private readonly HashSet<int> foundItemIds = new HashSet<int>();

        protected override void OnItemDataUpdated(LevelItemsItem item, LevelItemData data, int index)
        {
            bool found = data != null && foundItemIds.Contains(data.ItemId);
            item.SetChecked(found);
        }

        /// <summary>
        /// 设置已找到的物品 ID 列表，刷新显示状态。通常在加载关卡数据或恢复游戏状态时调用，参数为已找到物品的 ID 集合。
        /// </summary>
        /// <param name="itemIds"></param>
        public void SetFoundItems(IEnumerable<int> itemIds)
        {
            foundItemIds.Clear();

            if (itemIds != null)
            {
                foreach (int id in itemIds)
                {
                    foundItemIds.Add(id);
                }
            }

            Refresh();
        }

        /// <summary>
        /// 标记一个物品为已找到，刷新显示状态。通常在玩家拾取物品后调用，参数为物品 ID。
        /// </summary>
        /// <param name="itemId"></param>
        public void MarkItemFound(int itemId)
        {
            if (foundItemIds.Add(itemId))
            {
                Refresh();
            }
        }

        public bool MoveItemToEndAndMarkFound(int itemId, out UnityEngine.Vector3 worldPosition)
        {
            worldPosition = UnityEngine.Vector3.zero;

            foundItemIds.Add(itemId);

            int index = FindDataIndex(data => data != null && data.ItemId == itemId);
            if (index < 0)
            {
                Refresh();
                return false;
            }

            MoveDataToEnd(index, true);
            UnityEngine.Canvas.ForceUpdateCanvases();
            return TryGetItemWorldPositionById(itemId, out worldPosition);
        }

        /// <summary>
        /// 清除所有已找到的物品状态，刷新显示。通常在重新开始关卡或重置状态时调用。
        /// </summary>
        public void ClearFoundItems()
        {
            foundItemIds.Clear();
            Refresh();
        }

        /// <summary>
        /// 检查一个物品 ID 是否已标记为找到。可以用于外部逻辑判断或 UI 显示条件。
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public bool IsItemFound(int itemId)
        {
            return foundItemIds.Contains(itemId);
        }

        public bool TryGetItemWorldPositionById(int itemId, out UnityEngine.Vector3 worldPosition)
        {
            worldPosition = UnityEngine.Vector3.zero;

            for (int i = 0; i < DataCount; i++)
            {
                var data = GetData(i);
                if (data == null || data.ItemId != itemId) continue;
                return TryGetItemWorldPosition(i, out worldPosition);
            }

            return false;
        }
    }
}
