using UnityEngine;

namespace ZYQ.Demo
{
    public class MatchSystem : ManagerBase
    {
        private int found = 0;
        private int total = 6;

        public int Found => found;
        public int Total => total;
        public bool IsCompleted => found >= total;

        public void Hit()
        {
            if (IsCompleted) return;

            found++;
        }

        protected override void OnInit()
        {
            found = 0;
            Debug.Log($"MatchSystem Init: {found}/{total}");
        }

        protected override void OnDispose()
        {
            found = 0;
        }
    }
}
