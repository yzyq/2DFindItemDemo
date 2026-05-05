using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    [Serializable]
    public class LevelData
    {
        [SerializeField] private int levelId;
        [SerializeField] private int sortOrder;
        [SerializeField] private string levelName;
        [SerializeField] private Sprite levelIcon;

        [SerializeField] private Sprite background;
        [SerializeField] private Sprite foreground;

        [SerializeField] private AudioClip bgm;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        [SerializeField] private List<LevelItemData> items = new List<LevelItemData>();

        // 需要找到的物品，只存 itemId，必须来自当前关卡 items 列表
        [SerializeField] private List<int> targetItemIds = new List<int>();

        public int LevelId => levelId;
        public int SortOrder => sortOrder;
        public string LevelName => levelName;
        public Sprite LevelIcon => levelIcon;
        public Sprite Background => background;
        public Sprite Foreground => foreground;
        public AudioClip Bgm => bgm;
        public string Description => description;
        public IReadOnlyList<LevelItemData> Items => items;
        public IReadOnlyList<int> TargetItemIds => targetItemIds;

        public LevelItemData GetItem(int itemId)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].ItemId == itemId)
                {
                    return items[i];
                }
            }

            return null;
        }

        public bool ContainsTarget(int itemId)
        {
            return targetItemIds.Contains(itemId);
        }
    }
}
