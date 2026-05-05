using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZYQ.Demo
{
    public class LevelItemsItem : MonoBehaviour, IPoolScrollItem<LevelItemData>
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image checkedImage;

        private LevelItemData data;
        private int index;
        private bool isChecked;

        public LevelItemData Data => data;
        public int Index => index;
        public bool IsChecked => isChecked;

        public void SetData(LevelItemData itemData, int index)
        {
            data = itemData;
            this.index = index;

            if (nameText != null)
            {
                nameText.text = data != null && !string.IsNullOrEmpty(data.ItemName) ? data.ItemName : "未命名物品";
            }

            if (iconImage != null)
            {
                iconImage.sprite = data != null ? data.UiIcon : null;
                iconImage.enabled = data != null && data.UiIcon != null;
            }

            // 默认清理显示状态，真正状态由外部 ScrollView 根据 foundIds 刷新
            SetChecked(false);
        }

        public void SetChecked(bool value)
        {
            isChecked = value;

            if (checkedImage != null)
            {
                checkedImage.enabled = value;
            }
        }

        private void OnDisable()
        {
            // 对象池回收时清理视觉状态
            SetChecked(false);
        }
    }
}