using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Effect;
using GameJam.InGame.Monster;
using GameJam.InGame.System;
using UnityEngine;

namespace GameJam.InGame.Player
{
    // Runs the player state machine (Normal: slash projectile / Flow: dash slash) and owns the actions the states use.
    public class GJPlayerManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private GJInputManager _inputManager;
        private GJPlayerProjectileManager _projectileManager;
        private GJMonsterManager _monsterManager;
        private GJEffectManager _effectManager;
        private GJFlowGauge _flowGauge;
        private GJGameRecord _gameRecord;
        private GJSoundManager _soundManager;
        private GJPlayerComponent _playerComponent;
        private GJInGameParameter _inGameParameter;
        private GJPlayerStateManager _stateManager;
        private int _groundLayerMask;

        public bool IsInFlow => _stateManager.CurrentState is GJPlayerStateFlow;
        // Increases by one on every dash slash. Other managers compare it with their last seen value to react to dashes.
        public int FlowDashSequence { get; private set; }
        public Vector2 LastFlowDashDirection { get; private set; }

        public GJPlayerManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _inputManager = contextMap.GetRegisteredInstance<GJInputManager>();
            _projectileManager = contextMap.GetRegisteredInstance<GJPlayerProjectileManager>();
            _monsterManager = contextMap.GetRegisteredInstance<GJMonsterManager>();
            _effectManager = contextMap.GetRegisteredInstance<GJEffectManager>();
            _flowGauge = contextMap.GetRegisteredInstance<GJFlowGauge>();
            _gameRecord = contextMap.GetRegisteredInstance<GJGameRecord>();
            _soundManager = contextMap.GetRegisteredInstance<GJSoundManager>();
            _playerComponent = _parameter.AssetHolder.PlayerComponent;
            _inGameParameter = _parameter.InGameParameter;
            _groundLayerMask = LayerMask.GetMask(GJDefine.GROUND_LAYER_NAME);
            _stateManager = new GJPlayerStateManager(CreateNormalState());
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PlayerStateUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            _stateManager.UpdatePlayerState(deltaTime);
        }

        public bool TryGetRemainingFlowDashCount(out int remainingDashCount)
        {
            if (_stateManager.CurrentState is GJPlayerStateFlow flowState)
            {
                remainingDashCount = flowState.RemainingDashCount;
                return true;
            }

            remainingDashCount = 0;
            return false;
        }

        public IGJPlayerState CreateNormalState()
        {
            return new GJPlayerStateNormal(this, _inGameParameter, _flowGauge);
        }

        public IGJPlayerState CreateFlowState()
        {
            return new GJPlayerStateFlow(this, _inGameParameter, _inputManager);
        }

        public void FireSlashTowardMouse()
        {
            Vector2 playerCenter = _playerComponent.ColliderBounds.center;
            var direction = GetDirectionToMouse(playerCenter);
            var originPosition = playerCenter + direction * _inGameParameter.SlashProjectileSpawnDistance;
            _projectileManager.FireSlashProjectile(originPosition, direction);
            _soundManager.PlaySfxRandom(_parameter.AssetHolder.BasicAttackSfxClips);
        }

        public void BeginFlow()
        {
            _gameRecord.AddFlowEnterCount();
            _flowGauge.SetLocked(true);
            _playerComponent.SetVelocity(Vector2.zero);
            ApplyFlowGravity();
        }

        // Applied every frame so gravity edits in the inspector take effect immediately.
        // Runs after GJPlayerMovementManager (which sets the normal gravity), so this overrides it while in Flow.
        public void ApplyFlowGravity()
        {
            _playerComponent.SetGravityScale(_inGameParameter.PlayerGravityScale * _inGameParameter.FlowGravityScaleMultiplier);
        }

        // Teleports toward the mouse, stopping in front of walls, and returns the trail left behind.
        // Every monster on the trail takes FlowTrailDamage right away (once per dash).
        public GJFlowTrail DashTowardMouse()
        {
            var bounds = _playerComponent.ColliderBounds;
            Vector2 startCenter = bounds.center;
            var direction = GetDirectionToMouse(startCenter);
            var wallSkin = _inGameParameter.FlowDashWallSkin;
            var dashDistance = _inGameParameter.MaxFlowDashDistance;

            // Cast a box shrunk by the skin on each side so a wall or floor the player is already touching is not hit at distance 0.
            var castSize = new Vector2(
                Mathf.Max(bounds.size.x - wallSkin * 2f, 0.01f),
                Mathf.Max(bounds.size.y - wallSkin * 2f, 0.01f));
            var hit = Physics2D.BoxCast(startCenter, castSize, 0f, direction, dashDistance, _groundLayerMask);
            if (hit.collider != null)
            {
                // One skin to undo the shrink, one more to leave a gap from the wall.
                dashDistance = Mathf.Max(hit.distance - wallSkin * 2f, 0f);
            }

            var dashDelta = direction * dashDistance;
            _playerComponent.Teleport(_playerComponent.Position + dashDelta);
            LastFlowDashDirection = direction;
            FlowDashSequence++;
            _soundManager.PlaySfxRandom(_parameter.AssetHolder.FlowAttackSfxClips);
            var trail = _effectManager.CreateFlowTrail(startCenter, startCenter + dashDelta);

            var hitMonsters = _monsterManager.FindAllMonstersAlongSegment(
                trail.StartPosition,
                trail.EndPosition,
                _inGameParameter.FlowTrailHitWidth);
            foreach (var monster in hitMonsters)
            {
                monster.ApplyDamage(_inGameParameter.FlowTrailDamage);
            }
            return trail;
        }

        public bool IsOverlappingAnyMonster()
        {
            var playerCollider = _playerComponent.Collider;
            foreach (var monster in _monsterManager.SpawnedMonsters)
            {
                if (monster.IsOverlapping(playerCollider))
                {
                    return true;
                }
            }
            return false;
        }

        // The trails already dealt their damage when they were created, so they only fade out here.
        public void EndFlow(IReadOnlyList<GJFlowTrail> trails)
        {
            foreach (var trail in trails)
            {
                _effectManager.FadeOutFlowTrail(trail);
            }

            _flowGauge.ResetGauge();
            _flowGauge.SetLocked(false);
            _playerComponent.SetGravityScale(_inGameParameter.PlayerGravityScale);
        }

        private Vector2 GetDirectionToMouse(Vector2 fromPosition)
        {
            var toMouse = _inputManager.MouseWorldPosition - fromPosition;
            // Cursor exactly on the player: fall back to the facing direction.
            return toMouse.sqrMagnitude > Mathf.Epsilon
                ? toMouse.normalized
                : (_playerComponent.IsFacingRight ? Vector2.right : Vector2.left);
        }
    }
}
