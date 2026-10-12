using Cysharp.Threading.Tasks;

namespace GameJam.InGame.System
{
    public class GJGameStateManager : IGJContext
    {
        public GameState CurrentGameState { get; private set; } = new GameState.Initializing();

        public abstract record GameState
        {
            public record Initializing : GameState;
            public record GameProceeding : GameState;
            public record GameFinished : GameState;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
        }

        public void ResumeGame()
        {
            CurrentGameState = new GameState.GameProceeding();
        }

        public void FinishGame()
        {
            CurrentGameState = new GameState.GameFinished();
        }
    }
}
