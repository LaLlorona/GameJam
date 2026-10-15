using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameJam.InGame.System
{
    // Ends the game and shows the result window.
    public class GJGameResultManager : IGJContext
    {
        private readonly GJController.GJControllerParameter _parameter;
        private GJGameStateManager _gameStateManager;
        private GJGameRecord _gameRecord;
        private GJResultComponent _resultComponent;

        public GJGameResultManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _gameStateManager = contextMap.GetRegisteredInstance<GJGameStateManager>();
            _gameRecord = contextMap.GetRegisteredInstance<GJGameRecord>();
            _resultComponent = _parameter.AssetHolder.ResultComponent;
            if (_resultComponent == null)
            {
                Debug.LogError("GJAssetHolder has no result component assigned. The victory window will not be shown.");
                return;
            }
            _resultComponent.Hide();
        }

        public void FinishWithVictory()
        {
            if (_gameStateManager.CurrentGameState is GJGameStateManager.GameState.GameFinished)
            {
                return;
            }

            // The update loop stops here, so movement input no longer resets the velocity. Stop the player from sliding.
            _parameter.AssetHolder.PlayerComponent.SetVelocity(Vector2.zero);
            _gameStateManager.FinishGame();

            if (_resultComponent != null)
            {
                _resultComponent.ShowVictory(
                    damagedCount: _gameRecord.DamagedCount,
                    clearTime: _gameRecord.ElapsedTime,
                    flowEnterCount: _gameRecord.FlowEnterCount);
            }
        }
    }
}
