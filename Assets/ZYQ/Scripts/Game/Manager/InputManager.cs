using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace ZYQ.Demo
{
    public class InputManager : ManagerBase
    {
        public Vector2 Delta { get; private set; }
        public float Zoom { get; private set; }

        protected override void OnInit()
        {
            EnhancedTouchSupport.Enable();
            Debug.Log("InputManager Init");
        }

        protected override void OnDispose()
        {
            EnhancedTouchSupport.Disable();
            Debug.Log("InputManager OnDispose");
        }


        public override void Tick(float dt)
        {
            Delta = Vector2.zero;
            Zoom = 0f;

            var touches = Touchscreen.current?.touches;
            if (touches == null) return;

            TouchControlData first = default;
            TouchControlData second = default;
            int activeCount = 0;

            foreach (var touch in touches)
            {
                if (!touch.press.isPressed) continue;

                var data = new TouchControlData
                {
                    position = touch.position.ReadValue(),
                    delta = touch.delta.ReadValue()
                };

                if (activeCount == 0) first = data;
                else if (activeCount == 1) second = data;

                activeCount++;
                if (activeCount >= 2) break;
            }

            if (activeCount == 1)
            {
                Delta = first.delta;
            }
            else if (activeCount == 2)
            {
                float prev = (first.position - first.delta - (second.position - second.delta)).magnitude;
                float curr = (first.position - second.position).magnitude;

                Zoom = curr - prev;
            }
        }

        private struct TouchControlData
        {
            public Vector2 position;
            public Vector2 delta;
        }
    }



}
