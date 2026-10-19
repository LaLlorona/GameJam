using System;
using System.Collections.Generic;
using GameJam.InGame.Effect;
using GameJam.InGame.System;

namespace GameJam.InGame.Player
{
    // Dash slash: teleports toward the mouse every FlowDashInterval and leaves a trail.
    // Every dash damages the monsters on its trail right away. The trails stay visible until Flow ends.
    // Ends when the player lands on a monster, after FlowMaxDashCount dashes, or on a left click.
    public class GJPlayerStateFlow : IGJPlayerState
    {
        private readonly GJPlayerManager _playerManager;
        private readonly GJInGameParameter _inGameParameter;
        private readonly GJInputManager _inputManager;
        private readonly List<GJFlowTrail> _trails = new();
        private TimeSpan _elapsedTimeFromLastDash = TimeSpan.Zero;
        private bool _isFinished;

        public int UsedDashCount { get; private set; }
        public int RemainingDashCount => Math.Max(_inGameParameter.FlowMaxDashCount - UsedDashCount, 0);

        public GJPlayerStateFlow(GJPlayerManager playerManager, GJInGameParameter inGameParameter, GJInputManager inputManager)
        {
            _playerManager = playerManager;
            _inGameParameter = inGameParameter;
            _inputManager = inputManager;
        }

        public void OnEnter()
        {
            _playerManager.BeginFlow();
            // The first dash is used right on entering Flow.
            Dash();
        }

        public void OnExit()
        {
            _playerManager.EndFlow(_trails);
        }

        public void UpdateState(TimeSpan deltaTime)
        {
            if (_isFinished)
            {
                return;
            }

            if (_inputManager.IsMouseClickedThisFrame)
            {
                _isFinished = true;
                return;
            }

            _playerManager.ApplyFlowGravity();

            _elapsedTimeFromLastDash += deltaTime;
            if (_elapsedTimeFromLastDash < _inGameParameter.FlowDashInterval)
            {
                return;
            }

            _elapsedTimeFromLastDash = TimeSpan.Zero;
            Dash();
        }

        public bool TryGetNextState(out IGJPlayerState nextState)
        {
            if (_isFinished)
            {
                nextState = _playerManager.CreateNormalState();
                return true;
            }

            nextState = null;
            return false;
        }

        private void Dash()
        {
            _trails.Add(_playerManager.DashTowardMouse());
            UsedDashCount++;

            if (_playerManager.IsOverlappingAnyMonster() || UsedDashCount >= _inGameParameter.FlowMaxDashCount)
            {
                _isFinished = true;
            }
        }
    }
}
