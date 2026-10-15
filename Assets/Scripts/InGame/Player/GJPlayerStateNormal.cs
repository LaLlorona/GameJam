using System;
using GameJam.InGame.System;

namespace GameJam.InGame.Player
{
    // Fires a slash projectile toward the mouse cursor every SlashProjectileInterval.
    // Switches to Flow as soon as the Flow gauge is full.
    public class GJPlayerStateNormal : IGJPlayerState
    {
        private readonly GJPlayerManager _playerManager;
        private readonly GJInGameParameter _inGameParameter;
        private readonly GJFlowGauge _flowGauge;
        private TimeSpan _elapsedTimeFromLastSlash = TimeSpan.Zero;

        public GJPlayerStateNormal(GJPlayerManager playerManager, GJInGameParameter inGameParameter, GJFlowGauge flowGauge)
        {
            _playerManager = playerManager;
            _inGameParameter = inGameParameter;
            _flowGauge = flowGauge;
        }

        public void OnEnter()
        {
            _elapsedTimeFromLastSlash = TimeSpan.Zero;
        }

        public void OnExit()
        {
        }

        public void UpdateState(TimeSpan deltaTime)
        {
            _elapsedTimeFromLastSlash += deltaTime;
            if (_elapsedTimeFromLastSlash < _inGameParameter.SlashProjectileInterval)
            {
                return;
            }

            _elapsedTimeFromLastSlash = TimeSpan.Zero;
            _playerManager.FireSlashTowardMouse();
        }

        public bool TryGetNextState(out IGJPlayerState nextState)
        {
            if (_flowGauge.IsFull)
            {
                nextState = _playerManager.CreateFlowState();
                return true;
            }

            nextState = null;
            return false;
        }
    }
}
