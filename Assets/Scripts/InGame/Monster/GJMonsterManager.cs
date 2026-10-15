using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Player;
using GameJam.InGame.System;
using UnityEngine;

namespace GameJam.InGame.Monster
{
    // Owns every monster: spawns them, updates them each frame and removes them when they die.
    public class GJMonsterManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private readonly List<GJMonster> _spawnedMonsters = new();
        private readonly List<RaycastHit2D> _segmentCastResults = new();
        private GJPlayerManager _playerManager;
        private GJGameResultManager _gameResultManager;
        private TimeSpan _elapsedTimeFromStart = TimeSpan.Zero;
        private bool _isMonsterSpawned;

        public IReadOnlyList<GJMonster> SpawnedMonsters => _spawnedMonsters;

        public GJMonsterManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _playerManager = contextMap.GetRegisteredInstance<GJPlayerManager>();
            _gameResultManager = contextMap.GetRegisteredInstance<GJGameResultManager>();
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.MonsterUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            UpdateSpawn(deltaTime);

            // Monsters slow down while the player is in Flow. Time.timeScale is not used so the player keeps normal speed.
            var monsterDeltaTime = _playerManager.IsInFlow
                ? deltaTime * _parameter.InGameParameter.FlowMonsterTimeScale
                : deltaTime;
            foreach (var monster in _spawnedMonsters.ToArray())
            {
                monster.UpdateMonster(monsterDeltaTime);
            }
        }

        // Monsters touched by a circle of the given width swept from start to end. Each monster is returned once.
        public IReadOnlyCollection<GJMonster> FindAllMonstersAlongSegment(Vector2 startPosition, Vector2 endPosition, float width)
        {
            var segment = endPosition - startPosition;
            var length = segment.magnitude;
            var direction = length > Mathf.Epsilon ? segment / length : Vector2.right;
            // Monster colliders are triggers, so triggers must be included.
            var contactFilter = new ContactFilter2D { useTriggers = true };
            Physics2D.CircleCast(startPosition, width * 0.5f, direction, contactFilter, _segmentCastResults, length);

            var monstersOnSegment = new HashSet<GJMonster>();
            foreach (var hit in _segmentCastResults)
            {
                foreach (var monster in _spawnedMonsters)
                {
                    if (monster.IsDead == false && monster.HasCollider(hit.collider))
                    {
                        monstersOnSegment.Add(monster);
                        break;
                    }
                }
            }
            return monstersOnSegment;
        }

        // Spawns the monster once, MonsterSpawnDelay after the game starts.
        private void UpdateSpawn(TimeSpan deltaTime)
        {
            if (_isMonsterSpawned)
            {
                return;
            }

            _elapsedTimeFromStart += deltaTime;
            if (_elapsedTimeFromStart < _parameter.InGameParameter.MonsterSpawnDelay)
            {
                return;
            }

            _isMonsterSpawned = true;
            var assetHolder = _parameter.AssetHolder;
            if (assetHolder.MonsterPrefab == null || assetHolder.MonsterSpawnPoint == null)
            {
                Debug.LogError("GJAssetHolder has no monster prefab or monster spawn point assigned. No monster is spawned.");
                return;
            }
            SpawnMonster(assetHolder.MonsterPrefab, assetHolder.MonsterSpawnPoint.position);
        }

        public void SpawnMonster(GJMonsterComponent monsterPrefab, Vector2 spawnPosition)
        {
            _spawnedMonsters.Add(new GJMonster(
                monsterPrefab: monsterPrefab,
                spawnPosition: spawnPosition,
                playerComponent: _parameter.AssetHolder.PlayerComponent,
                inGameParameter: _parameter.InGameParameter,
                onDead: HandleMonsterDeath
            ));
        }

        private void HandleMonsterDeath(GJMonster monster)
        {
            _spawnedMonsters.Remove(monster);
            monster.DestroyMonsterComponent();

            // The player wins once every spawned monster is dead.
            if (_isMonsterSpawned && _spawnedMonsters.Count == 0)
            {
                _gameResultManager.FinishWithVictory();
            }
        }
    }
}
