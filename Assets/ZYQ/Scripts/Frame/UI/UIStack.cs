using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    // UIManager 的状态完全从这里读，自己不存当前面板

    public class UIStack
    {
        private readonly List<UIPanel> panels = new();

        public void Push(UIPanel panel)
        {
            if (panel == null)
                return;

            panels.Remove(panel);
            panels.Add(panel);
        }

        public UIPanel Pop()
        {
            if (panels.Count == 0)
                return null;

            int lastIndex = panels.Count - 1;
            UIPanel panel = panels[lastIndex];
            panels.RemoveAt(lastIndex);
            return panel;
        }

        public void Remove(UIPanel panel)
        {
            if (panel != null)
                panels.Remove(panel);
        }
    }

}
