using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Player;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace GameJam.InGame.System
{
    // The main camera is driven by the in-game virtual camera through the CinemachineBrain.
    // The virtual camera is a standalone scene object that follows the player with a fixed world offset.
    // Every Flow dash slash shakes the camera through a CinemachineCameraOffset applied after the confiner.
    public class GJCameraManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private readonly CinemachineCamera _inGameVirtualCamera;
        private readonly GJInGameParameter _inGameParameter;
        private CinemachineFollow _follow;
        private CinemachineConfiner2D _confiner;
        private float _appliedOrthographicSize;
        // 0 = normal orthographic size, 1 = Flow orthographic size.
        private float _flowZoomBlend;

        private GJPlayerManager _playerManager;
        private CinemachineCameraOffset _shakeOffset;
        private int _lastFlowDashSequence;
        private Vector2 _shakeDashDirection;
        private TimeSpan _shakeRemainingTime;
        // Random start point in the noise so every shake looks different.
        private float _shakeNoiseSeed;
        private float _shakeElapsedSeconds;

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
            SetUpShakeOffset();
            _playerManager = contextMap.GetRegisteredInstance<GJPlayerManager>();
            _lastFlowDashSequence = _playerManager.FlowDashSequence;
            ApplyParameter();
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.CameraUpdate;

        // Applied every frame so lens / offset edits in the inspector take effect immediately.
        public void GameUpdate(TimeSpan deltaTime)
        {
            var blendSeconds = (float)_inGameParameter.CameraFlowZoomBlendDuration.TotalSeconds;
            var target = _playerManager.IsInFlow ? 1f : 0f;
            _flowZoomBlend = blendSeconds > 0f
                ? Mathf.MoveTowards(_flowZoomBlend, target, (float)deltaTime.TotalSeconds / blendSeconds)
                : target;

            ApplyParameter();
            UpdateShake(deltaTime);
        }

        private void SetUpShakeOffset()
        {
            if (_inGameVirtualCamera.TryGetComponent(out _shakeOffset) == false)
            {
                _shakeOffset = _inGameVirtualCamera.gameObject.AddComponent<CinemachineCameraOffset>();
            }
            // After Body (follow + confiner), so the shake is not clamped away at the map edges.
            _shakeOffset.ApplyAfter = CinemachineCore.Stage.Noise;
            _shakeOffset.Offset = Vector3.zero;
        }

        private void UpdateShake(TimeSpan deltaTime)
        {
            var dashSequence = _playerManager.FlowDashSequence;
            if (dashSequence != _lastFlowDashSequence)
            {
                _lastFlowDashSequence = dashSequence;
                _shakeDashDirection = _playerManager.LastFlowDashDirection;
                _shakeRemainingTime = _inGameParameter.CameraShakeDuration;
                _shakeNoiseSeed = UnityEngine.Random.Range(0f, 100f);
                _shakeElapsedSeconds = 0f;
            }

            if (_shakeRemainingTime <= TimeSpan.Zero)
            {
                _shakeOffset.Offset = Vector3.zero;
                return;
            }

            _shakeElapsedSeconds += (float)deltaTime.TotalSeconds;
            var durationSeconds = (float)_inGameParameter.CameraShakeDuration.TotalSeconds;
            var ratio = durationSeconds > 0f
                ? Mathf.Clamp01((float)_shakeRemainingTime.TotalSeconds / durationSeconds)
                : 0f;
            // Squared so the shake starts hard and settles quickly.
            var strength = ratio * ratio;

            // Perlin noise instead of per-frame random so the shake stays smooth at any frame rate.
            var noiseTime = _shakeElapsedSeconds * _inGameParameter.CameraShakeFrequency;
            var noise = new Vector2(
                Mathf.PerlinNoise(_shakeNoiseSeed, noiseTime) * 2f - 1f,
                Mathf.PerlinNoise(_shakeNoiseSeed + 50f, noiseTime) * 2f - 1f);

            var offset = noise * _inGameParameter.CameraShakeAmplitude + _shakeDashDirection * _inGameParameter.CameraShakeDashKick;
            _shakeOffset.Offset = offset * strength;

            _shakeRemainingTime -= deltaTime;
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
            // Smoothstep so the zoom eases in and out instead of starting and stopping abruptly.
            var easedBlend = Mathf.SmoothStep(0f, 1f, _flowZoomBlend);
            var orthographicSize = Mathf.Lerp(_inGameParameter.CameraOrthographicSize, _inGameParameter.CameraFlowOrthographicSize, easedBlend);
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
