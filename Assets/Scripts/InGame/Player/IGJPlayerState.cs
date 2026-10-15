using System;

namespace GameJam.InGame.Player
{
    public interface IGJPlayerState
    {
        void OnEnter();
        void OnExit();
        void UpdateState(TimeSpan deltaTime);
        bool TryGetNextState(out IGJPlayerState nextState);
    }
}
