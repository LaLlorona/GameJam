using System;
using UnityEngine;

namespace GameJam.InGame.System
{
    // Gameplay tuning values. Managers read these every frame, so edits in the inspector apply immediately during play mode.
    public class GJInGameParameter : MonoBehaviour
    {
        [Header("Player Movement")]
        [SerializeField] private float _playerMoveSpeed = 7f;
        [SerializeField] private float _playerJumpVelocity = 12f;
        [SerializeField, Min(1)] private int _playerMaxJumpCount = 3;
        [SerializeField] private float _playerGravityScale = 3f;

        [Header("Ground Check")]
        [SerializeField, Min(0f)] private float _groundCheckDistance = 0.05f;
        // Checked slightly narrower than the collider so touching a wall isn't treated as grounded.
        [SerializeField, Range(0f, 1f)] private float _groundCheckWidthRatio = 0.9f;

        [Header("Camera")]
        [SerializeField, Min(0.01f)] private float _cameraOrthographicSize = 7f;
        // World offset of the virtual camera from the player.
        // y = 1 keeps the player slightly below the screen center, z = -10 keeps the camera in front of the sprites.
        [SerializeField] private Vector3 _cameraFollowOffset = new(0f, 1f, -10f);

        [Header("Slash Projectile")]
        // The player fires a slash projectile toward the mouse cursor every interval.
        [SerializeField, Min(0.01f)] private float _slashProjectileIntervalSeconds = 0.5f;
        [SerializeField, Min(0f)] private float _slashProjectileSpeed = 15f;
        [SerializeField, Min(0.01f)] private float _slashProjectileLifetimeSeconds = 1f;
        [SerializeField, Min(0f)] private float _slashProjectileScale = 1f;
        // Distance from the player center to the spawn point, along the fire direction.
        [SerializeField, Min(0f)] private float _slashProjectileSpawnDistance = 0.5f;
        [SerializeField, Min(0)] private int _slashProjectileDamage = 10;
        // When true the slash passes through monsters (each monster is hit once); otherwise it disappears on the first hit.
        [SerializeField] private bool _slashProjectilePierce = true;
        // Spin of the slash visual in degrees per second. Positive = counterclockwise. Does not affect the hit area.
        [SerializeField] private float _slashProjectileSpinSpeed = 1440f;
        // The slash is removed once it has spun this many degrees (or when its lifetime ends, whichever comes first).
        [SerializeField, Min(0f)] private float _slashProjectileMaxSpinDegrees = 120f;

        [Header("Flow Gauge")]
        [SerializeField, Min(0.01f)] private float _flowGaugeMax = 100f;
        // Added for every monster the player damages.
        [SerializeField, Min(0f)] private float _flowGaugeGainOnHit = 10f;
        // After this long without a hit, the gauge loses DecayAmountPerTick every DecayTickInterval.
        [SerializeField, Min(0f)] private float _flowGaugeDecayDelaySeconds = 2f;
        [SerializeField, Min(0.01f)] private float _flowGaugeDecayTickIntervalSeconds = 1f;
        [SerializeField, Min(0f)] private float _flowGaugeDecayAmountPerTick = 5f;

        [Header("Flow Dash Slash")]
        [SerializeField, Min(0.01f)] private float _flowDashIntervalSeconds = 0.3f;
        [SerializeField, Min(0f)] private float _flowDashDistance = 5f;
        // Gap kept between the player and a wall at the end of a dash.
        [SerializeField, Min(0f)] private float _flowDashWallSkin = 0.05f;
        [SerializeField, Min(1)] private int _flowMaxDashCount = 15;
        // Gravity while in Flow = PlayerGravityScale * this.
        [SerializeField, Min(0f)] private float _flowGravityScaleMultiplier = 0.3f;
        // Monster time scale while the player is in Flow.
        [SerializeField, Range(0f, 1f)] private float _flowMonsterTimeScale = 1f / 3f;

        [Header("Flow Trail")]
        // Damage per trail. A monster crossed by N trails takes this N times.
        [SerializeField, Min(0)] private int _flowTrailDamage = 30;
        [SerializeField, Min(0f)] private float _flowTrailHitWidth = 0.5f;
        [SerializeField, Min(0f)] private float _flowTrailFadeDurationSeconds = 0.3f;
        [SerializeField, Min(0f)] private float _flowTrailLineWidth = 0.15f;
        [SerializeField] private Color _flowTrailColor = new(0.6f, 1f, 1f, 1f);

        [Header("Monster")]
        // Time from game start until the monster is spawned at GJAssetHolder.MonsterSpawnPoint.
        [SerializeField, Min(0f)] private float _monsterSpawnDelaySeconds = 3f;
        // Read once when a monster is created.
        [SerializeField, Min(1)] private int _monsterMaxHp = 100;
        [SerializeField, Min(0f)] private float _monsterMoveSpeed = 3f;
        [SerializeField, Min(0f)] private float _monsterDamageBlinkDurationSeconds = 0.15f;
        [SerializeField] private Color _monsterDamageBlinkColor = new(1f, 0.3f, 0.3f, 1f);

        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float PlayerJumpVelocity => _playerJumpVelocity;
        public int PlayerMaxJumpCount => _playerMaxJumpCount;
        public float PlayerGravityScale => _playerGravityScale;

        public float GroundCheckDistance => _groundCheckDistance;
        public float GroundCheckWidthRatio => _groundCheckWidthRatio;

        public float CameraOrthographicSize => _cameraOrthographicSize;
        public Vector3 CameraFollowOffset => _cameraFollowOffset;

        public TimeSpan SlashProjectileInterval => TimeSpan.FromSeconds(_slashProjectileIntervalSeconds);
        public float SlashProjectileSpeed => _slashProjectileSpeed;
        public TimeSpan SlashProjectileLifetime => TimeSpan.FromSeconds(_slashProjectileLifetimeSeconds);
        public float SlashProjectileScale => _slashProjectileScale;
        public float SlashProjectileSpawnDistance => _slashProjectileSpawnDistance;
        public int SlashProjectileDamage => _slashProjectileDamage;
        public bool SlashProjectilePierce => _slashProjectilePierce;
        public float SlashProjectileSpinSpeed => _slashProjectileSpinSpeed;
        public float SlashProjectileMaxSpinDegrees => _slashProjectileMaxSpinDegrees;

        public float FlowGaugeMax => _flowGaugeMax;
        public float FlowGaugeGainOnHit => _flowGaugeGainOnHit;
        public TimeSpan FlowGaugeDecayDelay => TimeSpan.FromSeconds(_flowGaugeDecayDelaySeconds);
        public TimeSpan FlowGaugeDecayTickInterval => TimeSpan.FromSeconds(_flowGaugeDecayTickIntervalSeconds);
        public float FlowGaugeDecayAmountPerTick => _flowGaugeDecayAmountPerTick;

        public TimeSpan FlowDashInterval => TimeSpan.FromSeconds(_flowDashIntervalSeconds);
        public float FlowDashDistance => _flowDashDistance;
        public float FlowDashWallSkin => _flowDashWallSkin;
        public int FlowMaxDashCount => _flowMaxDashCount;
        public float FlowGravityScaleMultiplier => _flowGravityScaleMultiplier;
        public float FlowMonsterTimeScale => _flowMonsterTimeScale;

        public int FlowTrailDamage => _flowTrailDamage;
        public float FlowTrailHitWidth => _flowTrailHitWidth;
        public TimeSpan FlowTrailFadeDuration => TimeSpan.FromSeconds(_flowTrailFadeDurationSeconds);
        public float FlowTrailLineWidth => _flowTrailLineWidth;
        public Color FlowTrailColor => _flowTrailColor;

        public TimeSpan MonsterSpawnDelay => TimeSpan.FromSeconds(_monsterSpawnDelaySeconds);
        public int MonsterMaxHp => _monsterMaxHp;
        public float MonsterMoveSpeed => _monsterMoveSpeed;
        public TimeSpan MonsterDamageBlinkDuration => TimeSpan.FromSeconds(_monsterDamageBlinkDurationSeconds);
        public Color MonsterDamageBlinkColor => _monsterDamageBlinkColor;
    }
}
