using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJam.InGame.System
{
    // Runs first in the update loop and stores this frame's input state.
    // Other managers read these values instead of using callbacks.
    public class GJInputManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private Camera _mainCamera;

        public float HorizontalAxis { get; private set; }
        public bool IsJumpPressedThisFrame { get; private set; }
        public bool IsMouseClickedThisFrame { get; private set; }
        public Vector2 MouseWorldPosition { get; private set; }

        public GJInputManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _mainCamera = _parameter.MainCamera;
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.InputUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                var left = keyboard.aKey.isPressed ? 1f : 0f;
                var right = keyboard.dKey.isPressed ? 1f : 0f;
                HorizontalAxis = right - left;
                IsJumpPressedThisFrame = keyboard.spaceKey.wasPressedThisFrame;
            }
            else
            {
                HorizontalAxis = 0f;
                IsJumpPressedThisFrame = false;
            }

            if (mouse != null)
            {
                IsMouseClickedThisFrame = mouse.leftButton.wasPressedThisFrame;
                var screenPosition = mouse.position.ReadValue();
                var cameraDistance = -_mainCamera.transform.position.z;
                MouseWorldPosition = _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, cameraDistance));
            }
            else
            {
                IsMouseClickedThisFrame = false;
            }
        }
    }
}
