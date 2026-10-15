using System;

namespace GameJam.InGame.Monster
{
    public interface IGJMonsterState
    {
        void OnEnter();
        void OnExit();
        void UpdateState(TimeSpan deltaTime);
        bool TryGetNextState(out IGJMonsterState nextState);
    }
}
