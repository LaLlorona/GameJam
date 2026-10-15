using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace GameJam.InGame.System
{
    // The main camera is driven by the in-game virtual camera through the CinemachineBrain.
    // The virtual camera is a standalone scene object that follows the player with a fixed world offset.
    public class GJCameraManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private readonly CinemachineCamera _inGameVirtualCamera;
        private readonly GJInGameParameter _inGameParameter;
        private CinemachineFollow _follow;
        private CinemachineConfiner2D _confiner;
        private float _appliedOrthographicSize;

        public GJCameraManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
            _inGameVirtualCamera = parameter.AssetHolder.InGameVirtualCamera;
            _inGameParameter = parameter.InGameParameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            var mainCamera = _parameter.MainCamera;
            mainCamera.orthographic = true;
            if (mainCamera.TryGetComponent<CinemachineBrain>(out _) == false)
            {
                Debug.LogError($"Main camera '{mainCamera.name}' has no CinemachineBrain. Add one so the virtual camera can control it.");
            }

            if (_inGameVirtualCamera == null)
            {
                Debug.LogError("GJAssetHolder has no in-game virtual camera assigned.");
                return;
            }

            _inGameVirtualCamera.Follow = _parameter.AssetHolder.PlayerComponent.CameraFollowTransform;

            if (_inGameVirtualCamera.TryGetComponent<CinemachineFollow>(out var follow) == false)
            {
                follow = _inGameVirtualCamera.gameObject.AddComponent<CinemachineFollow>();
            }
            var trackerSettings = follow.TrackerSettings;
            // World space so the offset never rotates or flips with the player.
            trackerSettings.BindingMode = BindingMode.WorldSpace;
            // No damping: the camera sticks to the player.
            trackerSettings.PositionDamping = Vector3.zero;
            follow.TrackerSettings = trackerSettings;
            _follow = follow;

            SetUpConfiner(contextMap.GetRegisteredInstance<GJMapManager>().MapBoundsCollider);
            ApplyParameter();
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.CameraUpdate;

        // Applied every frame so lens / offset edits in the inspector take effect immediately.
        public void GameUpdate(TimeSpan deltaTime)
        {
            ApplyParameter();
        }

        // Keeps the camera view inside the LDtk map.
        private void SetUpConfiner(BoxCollider2D mapBoundsCollider)
        {
            if (mapBoundsCollider == null)
            {
                Debug.LogError("Map bounds collider is missing. The camera will not be confined to the map.");
                return;
            }

            if (_inGameVirtualCamera.TryGetComponent(out _confiner) == false)
            {
                _confiner = _inGameVirtualCamera.gameObject.AddComponent<CinemachineConfiner2D>();
            }
            _confiner.BoundingShape2D = mapBoundsCollider;
            // Without this, a view wider (or taller) than the map collapses the confiner to the map center point,
            // which also locks the other axis. Enabled, it keeps the camera moving along the axis that still fits.
            // MaxWindowSize 0 bakes every view size; cheap enough for a single box.
            _confiner.OversizeWindow = new CinemachineConfiner2D.OversizeWindowSettings
            {
                Enabled = true,
                MaxWindowSize = 0f,
                Padding = 0f,
            };
            _confiner.InvalidateBoundingShapeCache();
        }

        private void ApplyParameter()
        {
            var orthographicSize = _inGameParameter.CameraOrthographicSize;
            _inGameVirtualCamera.Lens.OrthographicSize = orthographicSize;
            _follow.FollowOffset = _inGameParameter.CameraFollowOffset;

            // The confiner caches the view size, so it has to be told when the lens changes.
            if (_confiner != null && Mathf.Approximately(_appliedOrthographicSize, orthographicSize) == false)
            {
                _confiner.InvalidateLensCache();
            }
            _appliedOrthographicSize = orthographicSize;
        }
    }
}
