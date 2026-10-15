using System;
using System.Collections.Generic;
using GameJam.InGame.Monster;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameJam.InGame.Player
{
    // Logic of a single player projectile. Moves straight in the fire direction until its lifetime ends.
    public class GJPlayerProjectile
    {
        private readonly GJPlayerProjectileComponent _projectileComponent;
        private readonly Vector2 _direction;
        private readonly float _speed;
        private readonly TimeSpan _lifetime;
        private readonly HashSet<GJMonster> _alreadyHitMonsters = new();
        private TimeSpan _elapsedTimeFromCreation = TimeSpan.Zero;
        private float _spunDegrees;

        public GJPlayerProjectile(
            GJPlayerProjectileComponent projectilePrefab,
            Vector2 originPosition,
            Vector2 direction,
            float speed,
            TimeSpan lifetime,
            float scale)
        {
            _direction = direction.normalized;
            _speed = speed;
            _lifetime = lifetime;
            _projectileComponent = Object.Instantiate(projectilePrefab);
            _projectileComponent.Init(originPosition, _direction, scale);
        }

        // spinSpeedDegreesPerSecond is passed every frame so inspector edits apply immediately.
        public void UpdateProjectile(TimeSpan deltaTime, float spinSpeedDegreesPerSecond)
        {
            _elapsedTimeFromCreation += deltaTime;
            var deltaSeconds = (float)deltaTime.TotalSeconds;
            _projectileComponent.Move(_direction * (_speed * deltaSeconds));
            var spinDegrees = spinSpeedDegreesPerSecond * deltaSeconds;
            _projectileComponent.SpinVisual(spinDegrees);
            _spunDegrees += Mathf.Abs(spinDegrees);
        }

        // Expires when its lifetime ends or once it has spun maxSpinDegrees, whichever comes first.
        public bool IsProjectileExpired(float maxSpinDegrees)
        {
            return _elapsedTimeFromCreation >= _lifetime || _spunDegrees >= maxSpinDegrees;
        }

        // A projectile hits the same monster only once.
        public bool TryHitMonster(GJMonster monster)
        {
            var hitCollider = _projectileComponent.HitCollider;
            if (hitCollider == null || _alreadyHitMonsters.Contains(monster) || monster.IsOverlapping(hitCollider) == false)
            {
                return false;
            }
            _alreadyHitMonsters.Add(monster);
            return true;
        }

        public void DestroyProjectileComponent()
        {
            _projectileComponent.DestroyComponent();
        }
    }
}
