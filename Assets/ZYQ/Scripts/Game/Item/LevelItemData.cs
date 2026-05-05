using System;
using UnityEngine;

namespace ZYQ.Demo
{
    /// <summary>
    /// 关卡中的物品数据，包含 UI 显示和场景内显示的图标，以及拾取后在场景内显示的预制体
    /// </summary>
    [Serializable]
    public class LevelItemData
    {
        [SerializeField] private int itemId;
        [SerializeField] private string itemName;
        [SerializeField] private Sprite uiIcon;
        [SerializeField] private Sprite sceneIcon;
        [SerializeField] private GameObject prefab;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        public int ItemId => itemId;
        public string ItemName => itemName;
        public Sprite UiIcon => uiIcon;
        public Sprite SceneIcon => sceneIcon;
        public GameObject Prefab => prefab;
        public string Description => description;

        public LevelItemData()
        {
        }

        public LevelItemData(int itemId, string itemName, Sprite uiIcon, Sprite sceneIcon = null, GameObject prefab = null, string description = null)
        {
            this.itemId = itemId;
            this.itemName = itemName;
            this.uiIcon = uiIcon;
            this.sceneIcon = sceneIcon;
            this.prefab = prefab;
            this.description = description;
        }
    }
}
