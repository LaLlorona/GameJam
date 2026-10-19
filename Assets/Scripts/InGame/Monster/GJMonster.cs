using System;
using GameJam.InGame.Player;
using GameJam.InGame.System;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using Object = UnityEngine.Object;


namespace GameJam.InGame.Monster
{
    // Logic of a single monster: HP, state machine and damage blink.
    public class GJMonster
    {
        private readonly GJMonsterComponent _monsterComponent;
        private readonly GJInGameParameter _inGameParameter;
        private readonly GJMonsterStateManager _stateManager;
        private readonly Action<GJMonster> _onDead;
        private TimeSpan _leftDamageBlinkTime = TimeSpan.Zero;

        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;
        public Vector2 Position => _monsterComponent.Position;

        public GJMonster(
            GJMonsterComponent monsterPrefab,
            Vector2 spawnPosition,
            GJPlayerComponent playerComponent,
            GJInGameParameter inGameParameter,
            Action<GJMonster> onDead)
        {
            _monsterComponent = Object.Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);
            _inGameParameter = inGameParameter;
            _onDead = onDead;
            MaxHp = inGameParameter.MonsterMaxHp;
            CurrentHp = MaxHp;
            _monsterComponent.Init();
            _stateManager = new GJMonsterStateManager(new GJMonsterStateChase(this, playerComponent));
        }

        // deltaTime is already scaled by the Flow slowdown, so every monster timer must use it.
        public void UpdateMonster(TimeSpan deltaTime)
        {
            if (IsDead)
            {
                return;
            }
            _stateManager.UpdateMonsterState(deltaTime);
            UpdateDamageBlink(deltaTime);
        }

        public void MoveToward(Vector2 targetPosition, TimeSpan deltaTime, float mult)
        {
            var maxDistance = _inGameParameter.MonsterMoveSpeed * (float)deltaTime.TotalSeconds * mult;
            _monsterComponent.MoveTowards(targetPosition, maxDistance);
            var deltaX = targetPosition.x - _monsterComponent.Position.x;
            if (Mathf.Approximately(deltaX, 0f) == false)
            {
                _monsterComponent.SetFacing(deltaX > 0f);
            }
        }

        public void SpawnProjectile(Vector2 targetPosition, TimeSpan deltaTime)
        {
            var maxDistance = _inGameParameter.MonsterMoveSpeed * (float)deltaTime.TotalSeconds;
            GameObject cannonBall = Object.Instantiate(_monsterComponent.projectile, _monsterComponent.transform.position, Quaternion.identity);
            cannonBall.SetActive(true);
            ProjectileScript proj = cannonBall.gameObject.GetComponent<ProjectileScript>();
            // Rigidbody rb = cannonBall.GetComponent<Rigidbody>();
            // rb.AddForce(cannonBall.transform.forward, ForceMode.Impulse);
            proj.setStuff(targetPosition, maxDistance);
            // proj.MoveTowards(targetPosition, maxDistance);
        }

        public bool IsOverlapping(Collider2D other)
        {
            return IsDead == false && _monsterComponent.IsOverlapping(other);
        }

        public bool HasCollider(Collider2D collider)
        {
            return _monsterComponent.Collider == collider;
        }

        public void ApplyDamage(int damage)
        {
            if (IsDead)
            {
                return;
            }

            CurrentHp = Mathf.Max(CurrentHp - damage, 0);
            _leftDamageBlinkTime = _inGameParameter.MonsterDamageBlinkDuration;
            _monsterComponent.SetDamageBlink(_inGameParameter.MonsterDamageBlinkColor, 1f);

            if (IsDead)
            {
                _onDead?.Invoke(this);
            }
        }

        public void DestroyMonsterComponent()
        {
            _monsterComponent.DestroyComponent();
        }

        private void UpdateDamageBlink(TimeSpan deltaTime)
        {
            if (_leftDamageBlinkTime <= TimeSpan.Zero)
            {
                return;
            }

            _leftDamageBlinkTime -= deltaTime;
            var blinkDuration = _inGameParameter.MonsterDamageBlinkDuration;
            var blinkRatio = blinkDuration > TimeSpan.Zero
                ? Mathf.Clamp01((float)(_leftDamageBlinkTime.TotalSeconds / blinkDuration.TotalSeconds))
                : 0f;
            _monsterComponent.SetDamageBlink(_inGameParameter.MonsterDamageBlinkColor, blinkRatio);
        }

        public Vector2 getPosition()
        {
            return _monsterComponent.getPosition();
        }

        
    }
}
