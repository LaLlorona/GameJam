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
        GameTimerUpdate = 7,
        PostProcessUpdate = 8,
        HUDUpdate = 9,
        CameraUpdate = 10,
        SoundUpdate = 11,
        EffectUpdate = 100,
    }

    public interface IGJPriorityUpdatable
    {
        GJUpdatePriority UpdatePriority { get; }
        void GameUpdate(TimeSpan deltaTime);
    }
}
