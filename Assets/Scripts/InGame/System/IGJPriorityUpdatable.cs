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
        PlayerProjectileUpdate = 5,
        PlayerHitCheckUpdate = 6,
        PostProcessUpdate = 7,
        HUDUpdate = 8,
        CameraUpdate = 9,
        EffectUpdate = 100,
    }

    public interface IGJPriorityUpdatable
    {
        GJUpdatePriority UpdatePriority { get; }
        void GameUpdate(TimeSpan deltaTime);
    }
}
