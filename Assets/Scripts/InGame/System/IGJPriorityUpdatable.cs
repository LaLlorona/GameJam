using System;

namespace GameJam.InGame.System
{
    // Lower values run first.
    public enum GJUpdatePriority
    {
        InputUpdate = 0,
        PlayerMovementUpdate = 1,
        PlayerStateUpdate = 2,
        FlowGaugeUpdate = 3,
        MonsterUpdate = 4,
        PlayerHitCheckUpdate = 5,
        PostProcessUpdate = 6,
        HUDUpdate = 7,
        EffectUpdate = 100,
    }

    public interface IGJPriorityUpdatable
    {
        GJUpdatePriority UpdatePriority { get; }
        void GameUpdate(TimeSpan deltaTime);
    }
}
