using System;

namespace GameJam.InGame.Player
{
    public class GJPlayerStateManager
    {
        public IGJPlayerState CurrentState { get; private set; }

        public GJPlayerStateManager(IGJPlayerState initialState)
        {
            ChangeState(initialState);
        }

        // The transition is checked after the update so a state can finish (e.g. Flow end) in the same frame.
        public void UpdatePlayerState(TimeSpan deltaTime)
        {
            CurrentState.UpdateState(deltaTime);
            if (CurrentState.TryGetNextState(out var nextState))
            {
                ChangeState(nextState);
            }
        }

        private void ChangeState(IGJPlayerState newState)
        {
            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState.OnEnter();
        }
    }
}
