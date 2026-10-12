using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace GameJam.InGame.System
{
    public class GJUpdateLoopManager : IGJContext
    {
        private readonly List<IGJPriorityUpdatable> _updateList = new();
        private bool _isSorted;
        private GJGameStateManager _gameStateManager;

        public void RegisterUpdatable(IGJPriorityUpdatable updatable)
        {
            _updateList.Add(updatable);
            _isSorted = false;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _gameStateManager = contextMap.GetRegisteredInstance<GJGameStateManager>();
        }

        public void UpdateInGameLoop(TimeSpan deltaTime)
        {
            if (_gameStateManager.CurrentGameState is not GJGameStateManager.GameState.GameProceeding)
            {
                return;
            }

            UpdateAll(deltaTime);
        }

        private void UpdateAll(TimeSpan deltaTime)
        {
            if (!_isSorted)
            {
                _updateList.Sort((a, b) => a.UpdatePriority.CompareTo(b.UpdatePriority));
                _isSorted = true;
            }

            foreach (var updatable in _updateList)
            {
                updatable.GameUpdate(deltaTime);
            }
        }
    }
}
