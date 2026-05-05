using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZYQ.Demo
{
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] private UIPanelType type;

        protected AppContext context;
        public UIPanelType Type { get => type; set => type = value; }

        public void Bind(AppContext ctx) => context = ctx;

        public virtual UniTask ShowAsync()
        {
            gameObject.SetActive(true);
            OnShow();
            return UniTask.CompletedTask;
        }

        public virtual UniTask HideAsync()
        {
            OnHide();
            gameObject.SetActive(false);
            return UniTask.CompletedTask;
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }


}
