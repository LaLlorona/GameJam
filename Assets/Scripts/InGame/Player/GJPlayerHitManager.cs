using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Monster;
using GameJam.InGame.System;

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
            _playerComponent = _parameter.AssetHolder.PlayerComponent;
            _inGameParameter = _parameter.InGameParameter;
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PlayerHitCheckUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            if (IsInvincible)
            {
                UpdateInvincible(deltaTime);
                return;
            }

            if (IsTouchingAnyMonster())
            {
                OnDamaged();
            }

            //could add to the previous just separating it if it causes future problems
            if (_playerComponent.isHit)
            {
                _playerComponent.isHit = false;
                OnDamaged();
            }
        }

        private bool IsTouchingAnyMonster()
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

        private void OnDamaged()
        {
            _gameRecord.AddDamagedCount();
            _flowGauge.LoseFlowOnDamaged();
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
