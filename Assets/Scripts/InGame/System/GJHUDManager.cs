using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Monster;
using GameJam.InGame.Player;
using UnityEngine;

namespace GameJam.InGame.System
{
    // Reads game state from the other managers every frame and shows it on GJHUDComponent.
    public class GJHUDManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private GJHUDComponent _hudComponent;
        private GJMonsterManager _monsterManager;
        private GJPlayerManager _playerManager;
        private GJFlowGauge _flowGauge;
        private GJGameRecord _gameRecord;

        public GJHUDManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _hudComponent = _parameter.AssetHolder.HUDComponent;
            if (_hudComponent == null)
            {
                Debug.LogError("GJAssetHolder has no HUD component assigned. The HUD will not be updated.");
                return;
            }

            _monsterManager = contextMap.GetRegisteredInstance<GJMonsterManager>();
            _playerManager = contextMap.GetRegisteredInstance<GJPlayerManager>();
            _flowGauge = contextMap.GetRegisteredInstance<GJFlowGauge>();
            _gameRecord = contextMap.GetRegisteredInstance<GJGameRecord>();
            // Shown once the monster spawns.
            _hudComponent.SetHudVisible(false);
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.HUDUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            _hudComponent.SetElapsedTime(_gameRecord.ElapsedTime);
            UpdateFlow();
            UpdateMonsterHp();
        }

        private void UpdateFlow()
        {
            var isInFlow = _playerManager.TryGetRemainingFlowDashCount(out var remainingDashCount);
            // The gauge stays full during Flow and is reset to 0 when Flow ends.
            _hudComponent.SetFlowGauge(isInFlow ? 1f : _flowGauge.FlowRatio);
            _hudComponent.SetFlowDashCount(isInFlow, remainingDashCount);
        }

        // Shows the HP of the first spawned monster (the boss).
        private void UpdateMonsterHp()
        {
            var spawnedMonsters = _monsterManager.SpawnedMonsters;
            if (spawnedMonsters.Count == 0)
            {
                return;
            }

            var monster = spawnedMonsters[0];
            _hudComponent.SetHudVisible(true);
            _hudComponent.SetMonsterHp(monster.CurrentHp, monster.MaxHp);
        }
    }
}
