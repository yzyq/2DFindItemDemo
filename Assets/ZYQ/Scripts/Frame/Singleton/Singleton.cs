using System;
using UnityEngine;
namespace ZYQ.Demo
{
    #region  Singleton
    public abstract class Singleton<T> : IDisposable where T : new()
    {
        private static T mInstance;
        private static readonly object sync = new object();

        protected Singleton() { }

        public static T Instance
        {
            get
            {
                if (mInstance == null)
                {
                    lock (sync)
                    {
                        if (mInstance == null)   // ✔ 双重检查
                        {
                            mInstance = new T();
                        }
                    }
                }
                return mInstance;
            }
            set
            {
                mInstance = value;
            }
        }

        public virtual void Dispose()
        {
            mInstance = default;
        }
    }
    #endregion

    // 1. 定义一个非泛型的基类，用来统一处理全局标记
    public abstract class MonoSingletonGlobal
    {
        // 标记程序是否正在退出（静态变量，非泛型，容易重置）
        public static bool mAppQuitting = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGlobalStatics()
        {
            mAppQuitting = false;
        }
    }

    #region MonoSingleton
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        protected static T mInstance;

        private static readonly object mLock = new object();

        protected virtual bool IsGlobal => true;

        public static T Instance
        {
            get
            {
                if (MonoSingletonGlobal.mAppQuitting) return null;

                if (mInstance == null)
                {
                    lock (mLock)
                    {
                        if (mInstance == null)
                        {
                            mInstance = FindFirstObjectByType<T>();

                            // ❗ 不再自动创建（避免生命周期混乱）
                            if (mInstance == null)
                            {
                                throw new Exception($"{typeof(T)} not found in scene!");
                            }
                        }
                    }
                }

                return mInstance;
            }
        }

        protected virtual void Awake()
        {
            if (mInstance == null)
            {
                mInstance = this as T;

                if (IsGlobal)
                    DontDestroyOnLoad(gameObject);
            }
            else if (mInstance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (mInstance == this)
            {
                if (!IsGlobal)
                {
                    mInstance = null;
                }
            }
        }

        public static bool TryGet(out T instance)
        {
            instance = Instance;
            return instance != null;
        }
    }
    #endregion
}
