using UnityEngine;

namespace ZYQ.Demo
{
    public abstract class ManagerBase
    {
        public AppContext Context { get; private set; }
        public bool IsInitialized { get; private set; }

        public void Init(AppContext context)
        {
            if (IsInitialized) return;

            Context = context;
            OnInit();
            IsInitialized = true;
        }

        public virtual void Tick(float dt) { }   // 统一驱动Update，不用每个Manager挂MonoBehaviour

        public virtual void Dispose()
        {
            if (!IsInitialized) return;

            OnDispose();
            IsInitialized = false;
            Context = null;
        }

        public void Reinitialize(AppContext context)
        {
            Dispose();
            Init(context);
        }

        protected virtual void OnInit() { }
        protected virtual void OnDispose() { }
    }

}
