using System;

namespace GameJam.InGame.Monster
{
    public class GJMonsterStateManager
    {
        public IGJMonsterState CurrentState { get; private set; }

        public GJMonsterStateManager(IGJMonsterState initialState)
        {
            ChangeState(initialState);
        }

        public void UpdateMonsterState(TimeSpan deltaTime)
        {
            if (CurrentState.TryGetNextState(out var nextState))
            {
                ChangeState(nextState);
            }
            CurrentState.UpdateState(deltaTime);
        }

        private void ChangeState(IGJMonsterState newState)
        {
            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState.OnEnter();
        }
    }
}
