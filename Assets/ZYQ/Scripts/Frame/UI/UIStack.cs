using System.Collections.Generic;
using UnityEngine;

namespace ZYQ.Demo
{
    // UIManager 的状态完全从这里读，自己不存当前面板

    public class UIStack
    {
        private Stack<UIPanel> stack = new();

        public void Push(UIPanel panel) => stack.Push(panel);

        public UIPanel Pop()
        {
            if (stack.Count == 0) return null;
            return stack.Pop();
        }
    }

}
