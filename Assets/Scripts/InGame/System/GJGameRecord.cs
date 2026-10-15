using System;
using Cysharp.Threading.Tasks;

namespace GameJam.InGame.System
{
    // Play statistics shown on the HUD timer and the victory window.
    // The timer only advances while the update loop runs, so it stops by itself when the game finishes.
    public class GJGameRecord : IGJContext, IGJPriorityUpdatable
    {
        public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;
        public int DamagedCount { get; private set; }
        public int FlowEnterCount { get; private set; }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.GameTimerUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            ElapsedTime += deltaTime;
        }

        public void AddDamagedCount()
        {
            DamagedCount++;
        }

        public void AddFlowEnterCount()
        {
            FlowEnterCount++;
        }

        // mm:ss.ff
        public static string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
        }
    }
}
