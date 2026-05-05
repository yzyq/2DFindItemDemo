using UnityEngine;
using System;
using System.Collections.Generic;

namespace ZYQ.Demo
{
    // 类型安全，无魔法字符串，订阅者弱引用（用 Action 即可，注意取消订阅）
    public static class EventDispatcher
    {
        private static Dictionary<Type, Delegate> events = new();

        public static void Subscribe<T>(Action<T> cb)
        {
            if (events.TryGetValue(typeof(T), out var del))
                events[typeof(T)] = Delegate.Combine(del, cb);
            else
                events[typeof(T)] = cb;
        }

        public static void UnSubscribe<T>(Action<T> cb)
        {
            if (events.TryGetValue(typeof(T), out var del))
                events[typeof(T)] = Delegate.Remove(del, cb);
        }

        public static void Publish<T>(T e)
        {
            if (events.TryGetValue(typeof(T), out var del))
                ((Action<T>)del)?.Invoke(e);
        }

        public static void Tick() { }

        public static void Clear()
        {
            events.Clear();
        }
    }


}
