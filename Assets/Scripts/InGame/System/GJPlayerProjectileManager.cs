using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Monster;
using GameJam.InGame.Player;
using UnityEngine;

namespace GameJam.InGame.System
{
    // Owns every player projectile: moves them each frame and removes them when their lifetime ends.
    public class GJPlayerProjectileManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private readonly List<GJPlayerProjectile> _projectiles = new();
        private GJInGameParameter _inGameParameter;
        private GJMonsterManager _monsterManager;
        private GJFlowGauge _flowGauge;

        public GJPlayerProjectileManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _inGameParameter = _parameter.InGameParameter;
            _monsterManager = contextMap.GetRegisteredInstance<GJMonsterManager>();
            _flowGauge = contextMap.GetRegisteredInstance<GJFlowGauge>();
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PlayerProjectileUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            foreach (var projectile in _projectiles.ToArray())
            {
                projectile.UpdateProjectile(deltaTime, _inGameParameter.SlashProjectileSpinSpeed);
                if (projectile.IsProjectileExpired(_inGameParameter.SlashProjectileMaxSpinDegrees))
                {
                    RemoveProjectile(projectile);
                }
            }

            // Projectiles are moved by transform, so push their new poses to the physics world before the overlap checks.
            Physics2D.SyncTransforms();
            foreach (var projectile in _projectiles.ToArray())
            {
                CheckMonsterHit(projectile);
            }
        }

        private void CheckMonsterHit(GJPlayerProjectile projectile)
        {
            // Copied because a monster that dies is removed from the list during the loop.
            foreach (var monster in _monsterManager.SpawnedMonsters.ToArray())
            {
                if (projectile.TryHitMonster(monster) == false)
                {
                    continue;
                }

                monster.ApplyDamage(_inGameParameter.SlashProjectileDamage);
                // Every monster hit fills the Flow gauge, so a piercing slash can fill it several times.
                _flowGauge.AddFlowOnHit();
                if (_inGameParameter.SlashProjectilePierce == false)
                {
                    RemoveProjectile(projectile);
                    return;
                }
            }
        }

        public void FireSlashProjectile(Vector2 originPosition, Vector2 direction)
        {
            var projectilePrefab = _parameter.AssetHolder.SlashProjectilePrefab;
            if (projectilePrefab == null)
            {
                Debug.LogError("GJAssetHolder has no slash projectile prefab assigned.");
                return;
            }

            var projectile = new GJPlayerProjectile(
                projectilePrefab: projectilePrefab,
                originPosition: originPosition,
                direction: direction,
                speed: _inGameParameter.SlashProjectileSpeed,
                lifetime: _inGameParameter.SlashProjectileLifetime,
                scale: _inGameParameter.SlashProjectileScale
            );
            _projectiles.Add(projectile);
        }

        public void RemoveProjectile(GJPlayerProjectile projectile)
        {
            _projectiles.Remove(projectile);
            projectile.DestroyProjectileComponent();
        }

        public void RemoveAllProjectiles()
        {
            foreach (var projectile in _projectiles)
            {
                projectile.DestroyProjectileComponent();
            }
            _projectiles.Clear();
        }
    }
}
