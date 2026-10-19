using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Monster;
using GameJam.InGame.System;
using UnityEngine;

namespace GameJam.InGame.Player
{
    // Damages the player on contact with a monster. No knockback.
    // After a hit the player is invincible for PlayerInvincibleDuration and the sprite blinks.
    public class GJPlayerHitManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private GJMonsterManager _monsterManager;
        private GJFlowGauge _flowGauge;
        private GJGameRecord _gameRecord;
        private GJPlayerComponent _playerComponent;
        private GJInGameParameter _inGameParameter;
        private GJPlayerMovementManager _movementManager;
        private TimeSpan _leftInvincibleTime = TimeSpan.Zero;

        public bool IsInvincible => _leftInvincibleTime > TimeSpan.Zero;

        public GJPlayerHitManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _monsterManager = contextMap.GetRegisteredInstance<GJMonsterManager>();
            _flowGauge = contextMap.GetRegisteredInstance<GJFlowGauge>();
            _gameRecord = contextMap.GetRegisteredInstance<GJGameRecord>();
            _movementManager = contextMap.GetRegisteredInstance<GJPlayerMovementManager>();
            _playerComponent = _parameter.AssetHolder.PlayerComponent;
            _inGameParameter = _parameter.InGameParameter;
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PlayerHitCheckUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            var touchingMonster = IsTouchingAnyMonster();
            if (IsInvincible)
            {
                UpdateInvincible(deltaTime);
                return;
            }

            if (touchingMonster != null)
            {
                OnDamaged(touchingMonster);
                return;
            }

            //could add to the previous just separating it if it causes future problems
            if (_playerComponent.isHit)
            {
                _playerComponent.isHit = false;
                OnDamaged(null);
            }
        }

        private GJMonster IsTouchingAnyMonster()
        {
            var playerCollider = _playerComponent.Collider;
            foreach (var monster in _monsterManager.SpawnedMonsters)
            {
                if (monster.IsOverlapping(playerCollider))
                {
                    return monster;
                }
            }
            return null;
        }

        private void OnDamaged(GJMonster monster)
        {
            _gameRecord.AddDamagedCount();
            _flowGauge.LoseFlowOnDamaged();
            Vector2 sourcePosition;
            if(monster != null)
            {
                sourcePosition = monster.Position;
            }
            else
            {
                sourcePosition = _playerComponent.HitSourcePosition;
            }
            Vector2 direction = (Vector2)_playerComponent.transform.position - sourcePosition;
            if(direction.sqrMagnitude > 0f)
            {
                direction.Normalize();
                _movementManager.ApplyKnockback(direction, 10f, 0.4f);
            }
            _leftInvincibleTime = _inGameParameter.PlayerInvincibleDuration;
        }

        private void UpdateInvincible(TimeSpan deltaTime)
        {
            _leftInvincibleTime -= deltaTime;
            if (IsInvincible == false)
            {
                _playerComponent.SetSpriteVisible(true);
                return;
            }

            // Toggle visibility every blink interval, counted from the moment of the hit.
            var elapsedSeconds = (_inGameParameter.PlayerInvincibleDuration - _leftInvincibleTime).TotalSeconds;
            var blinkIntervalSeconds = _inGameParameter.PlayerInvincibleBlinkInterval.TotalSeconds;
            var isVisible = blinkIntervalSeconds <= 0d || (int)(elapsedSeconds / blinkIntervalSeconds) % 2 == 1;
            _playerComponent.SetSpriteVisible(isVisible);
        }
    }
}
