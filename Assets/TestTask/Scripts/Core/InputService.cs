using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TestTask.Core
{
    public sealed class InputService
    {
        private bool _wasAiming;
        private float _lastAimX;

        public bool TapStarted()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                        return true;
                }
            }

            return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                return true;

            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }

        public float HorizontalAim()
        {
#if ENABLE_INPUT_SYSTEM
            var pressed = false;
            var position = Vector2.zero;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = true;
            }
            else if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                position = Pointer.current.position.ReadValue();
                pressed = true;
            }

            return pressed ? Mathf.InverseLerp(0f, Screen.width, position.x) * 2f - 1f : 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
                return Mathf.InverseLerp(0f, Screen.width, Input.GetTouch(0).position.x) * 2f - 1f;

            if (Input.GetMouseButton(0))
                return Mathf.InverseLerp(0f, Screen.width, Input.mousePosition.x) * 2f - 1f;

            return 0f;
#else
            return 0f;
#endif
        }

        public float HorizontalAimDelta()
        {
            if (!TryReadPointer(out var position))
            {
                _wasAiming = false;
                return 0f;
            }

            var currentX = position.x;
            if (!_wasAiming)
            {
                _wasAiming = true;
                _lastAimX = currentX;
                return 0f;
            }

            var delta = Screen.width > 0 ? (currentX - _lastAimX) / Screen.width : 0f;
            _lastAimX = currentX;
            return delta;
        }

        private static bool TryReadPointer(out Vector2 position)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Pointer.current != null && Pointer.current.press.isPressed)
            {
                position = Pointer.current.position.ReadValue();
                return true;
            }

            position = Vector2.zero;
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                position = Input.GetTouch(0).position;
                return true;
            }

            if (Input.GetMouseButton(0))
            {
                position = Input.mousePosition;
                return true;
            }

            position = Vector2.zero;
            return false;
#else
            position = Vector2.zero;
            return false;
#endif
        }
    }
}
