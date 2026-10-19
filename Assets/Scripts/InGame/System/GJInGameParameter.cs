using System;
using UnityEngine;
using UnityEngine.Serialization;

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
        // Orthographic size while in Flow. Blended with CameraOrthographicSize over CameraFlowZoomBlendDuration on enter / exit.
        [SerializeField, Min(0.01f)] private float _cameraFlowOrthographicSize = 9f;
        [SerializeField, Min(0f)] private float _cameraFlowZoomBlendDurationSeconds = 0.3f;
        // World offset of the virtual camera from the player.
        // y = 1 keeps the player slightly below the screen center, z = -10 keeps the camera in front of the sprites.
        [SerializeField] private Vector3 _cameraFollowOffset = new(0f, 1f, -10f);

        [Header("Camera Shake (Flow Dash)")]
        // Played on every dash slash. Shake strength fades out (squared) over the duration.
        [SerializeField, Min(0f)] private float _cameraShakeDurationSeconds = 0.15f;
        // Max random offset in world units.
        [SerializeField, Min(0f)] private float _cameraShakeAmplitude = 0.25f;
        // Noise speed. Higher = more violent jitter.
        [SerializeField, Min(0f)] private float _cameraShakeFrequency = 30f;
        // Extra push of the camera along the dash direction at the start of the shake.
        [SerializeField] private float _cameraShakeDashKick = 0.2f;

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

        [Header("Player Hit")]
        // Invincible time after touching a monster. Without it a single contact would count as a hit every frame.
        [SerializeField, Min(0f)] private float _playerInvincibleDurationSeconds = 1.5f;
        [SerializeField, Min(0f)] private float _playerInvincibleBlinkIntervalSeconds = 0.1f;

        [Header("Flow Gauge")]
        [SerializeField, Min(0.01f)] private float _flowGaugeMax = 100f;
        // Added for every monster the player damages.
        [SerializeField, Min(0f)] private float _flowGaugeGainOnHit = 10f;
        // Removed when the player is hit by a monster.
        [SerializeField, Min(0f)] private float _flowGaugeLossOnDamaged = 5f;
        // After this long without a hit, the gauge loses DecayAmountPerTick every DecayTickInterval.
        [SerializeField, Min(0f)] private float _flowGaugeDecayDelaySeconds = 2f;
        [SerializeField, Min(0.01f)] private float _flowGaugeDecayTickIntervalSeconds = 1f;
        [SerializeField, Min(0f)] private float _flowGaugeDecayAmountPerTick = 5f;

        [Header("Flow Dash Slash")]
        [SerializeField, Min(0.01f)] private float _flowDashIntervalSeconds = 0.3f;
        // Distance of one dash toward the cursor. A wall in the way shortens it.
        [SerializeField, Min(0f), FormerlySerializedAs("_flowDashDistance")] private float _maxFlowDashDistance = 5f;
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

        [Header("Flow Post Process")]
        // Time to blend the Flow volume weight 0 -> 1 on enter and 1 -> 0 on exit.
        [SerializeField, Min(0f)] private float _flowPostProcessBlendDurationSeconds = 0.2f;
        // Before Flow, the volume weight follows the gauge up to this value (gauge full = this weight).
        [SerializeField, Range(0f, 1f)] private float _flowGaugePreviewMaxWeight = 0.15f;
        // Time for the gauge preview to go from 0 to FlowGaugePreviewMaxWeight (smaller steps are proportionally faster).
        [SerializeField, Min(0f)] private float _flowGaugePreviewBlendDurationSeconds = 0.5f;
        // Color Adjustments: bright, contrasty, red-orange.
        [SerializeField] private float _flowPostExposure = 0.7f;
        [SerializeField, Range(-100f, 100f)] private float _flowContrast = 35f;
        [SerializeField, ColorUsage(false, true)] private Color _flowColorFilter = new(1f, 0.55f, 0.4f, 1f);
        [SerializeField, Range(-100f, 100f)] private float _flowSaturation = 20f;
        // White Balance: warm image with a magenta tint (purple sky).
        [SerializeField, Range(-100f, 100f)] private float _flowWhiteBalanceTemperature = 50f;
        [SerializeField, Range(-100f, 100f)] private float _flowWhiteBalanceTint = 15f;
        // Shadows Midtones Highlights: dark red shadows, pinkish violet highlights.
        [SerializeField] private Color _flowShadowsColor = new(1f, 0.6f, 0.6f, 1f);
        [SerializeField, Range(-1f, 1f)] private float _flowShadowsOffset = -0.1f;
        [SerializeField] private Color _flowHighlightsColor = new(1f, 0.85f, 1f, 1f);
        [SerializeField, Range(-1f, 1f)] private float _flowHighlightsOffset = 0.1f;
        // Bloom: blown-out highlights. Needs HDR enabled on the URP asset.
        [SerializeField, Min(0f)] private float _flowBloomIntensity = 1.5f;
        [SerializeField, Min(0f)] private float _flowBloomThreshold = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _flowBloomScatter = 0.7f;
        [SerializeField] private Color _flowBloomTint = new(1f, 0.6f, 0.4f, 1f);
        // Vignette
        [SerializeField, Range(0f, 1f)] private float _flowVignetteIntensity = 0.4f;
        [SerializeField, Range(0.01f, 1f)] private float _flowVignetteSmoothness = 0.4f;
        [SerializeField] private Color _flowVignetteColor = new(0.3f, 0f, 0f, 1f);
        // Chromatic Aberration
        [SerializeField, Range(0f, 1f)] private float _flowChromaticAberrationIntensity = 0.4f;
        // Lens Distortion kept during Flow. Negative = barrel (bulges outward).
        [SerializeField, Range(-1f, 1f)] private float _flowLensDistortionIntensity = 0f;
        // Extra lens distortion added on Flow enter, fading to 0 over FlowEnterPunchDuration.
        [SerializeField, Range(-1f, 1f)] private float _flowEnterPunchLensDistortion = -0.35f;
        [SerializeField, Min(0f)] private float _flowEnterPunchDurationSeconds = 0.3f;
        // Film Grain
        [SerializeField, Range(0f, 1f)] private float _flowFilmGrainIntensity = 0.2f;

        [Header("Flow Speed Effect")]
        // Radial blur. Strength = fraction of the distance to the player that a pixel is smeared over.
        [SerializeField, Range(0f, 1f)] private float _flowBlurStrength = 0.12f;
        // Distances are in screen heights from the player. Inside the inner radius the screen stays sharp.
        [SerializeField, Min(0f)] private float _flowBlurInnerRadius = 0.15f;
        [SerializeField, Min(0.001f)] private float _flowBlurFalloff = 0.45f;
        // Speed lines
        [SerializeField, Min(1f)] private float _flowLineCount = 120f;
        [SerializeField, Range(0f, 1f)] private float _flowLineDensity = 0.3f;
        [SerializeField, Range(0f, 0.5f)] private float _flowLineWidth = 0.15f;
        [SerializeField, Min(0f)] private float _flowLineInnerRadius = 0.35f;
        [SerializeField, Min(0.001f)] private float _flowLineFalloff = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _flowLineOpacity = 0.6f;
        [SerializeField] private Color _flowLineColor = new(1f, 0.95f, 0.9f, 1f);
        // Times per second the line pattern is re-rolled.
        [SerializeField, Min(0f)] private float _flowLineFlickerRate = 20f;
        // Extra intensity added on every dash slash, fading to 0 over FlowDashBurstDuration.
        [SerializeField, Min(0f)] private float _flowDashBurstIntensity = 0.8f;
        [SerializeField, Min(0f)] private float _flowDashBurstDurationSeconds = 0.15f;

        [Header("BGM")]
        // Volume of the "BGMVolume" mixer parameter in dB. Ignored when GJAssetHolder has no audio mixer.
        [SerializeField, Range(-80f, 0f)] private float _bgmMixerVolumeDb = 0f;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.6f;
        // Volume multiplier while muffled (outside Flow). 1 = same volume as in Flow.
        [SerializeField, Range(0f, 1f)] private float _bgmMuffledVolumeMultiplier = 0.6f;
        // Low pass cutoff in Hz. Muffled = "listening underwater" outside Flow, open = clear sound in Flow.
        [SerializeField, Range(10f, 22000f)] private float _bgmMuffledCutoffFrequency = 700f;
        [SerializeField, Range(10f, 22000f)] private float _bgmOpenCutoffFrequency = 22000f;
        [SerializeField, Range(1f, 10f)] private float _bgmMuffledResonanceQ = 1.5f;
        [SerializeField, Range(1f, 10f)] private float _bgmOpenResonanceQ = 1f;
        // Time to blend between muffled and open when entering / leaving Flow.
        [SerializeField, Min(0f)] private float _bgmFlowBlendDurationSeconds = 0.4f;

        [Header("SFX")]
        // Volume of the "SFXVolume" mixer parameter in dB. Ignored when GJAssetHolder has no audio mixer.
        // Normal = outside Flow, Flow = inside Flow. They are blended over BgmFlowBlendDuration when entering / leaving Flow.
        [SerializeField, Range(-80f, 20f)] private float _sfxMixerVolumeDb = 0f;
        [SerializeField, Range(-80f, 20f)] private float _sfxFlowMixerVolumeDb = 3f;
        // Each play is pitched randomly within 1 +/- this value so repeated sounds do not feel identical.
        [SerializeField, Range(0f, 0.5f)] private float _sfxPitchVariation = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        // Read once at start. Number of sounds that can overlap.
        [SerializeField, Min(1)] private int _sfxPoolSize = 8;

        [Header("HUD")]
        // Monster HP trail (white bar). It waits this long after the first damage before it starts to drain.
        [SerializeField, Min(0f)] private float _hpTrailHoldSeconds = 0.5f;
        // Drain speed in bar widths per second. The speed is the larger of the minimum speed and gap * speed per gap.
        [SerializeField, Min(0f)] private float _hpTrailMinSpeed = 0.3f;
        [SerializeField, Min(0f)] private float _hpTrailSpeedPerGap = 3f;

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
        public float CameraFlowOrthographicSize => _cameraFlowOrthographicSize;
        public TimeSpan CameraFlowZoomBlendDuration => TimeSpan.FromSeconds(_cameraFlowZoomBlendDurationSeconds);
        public Vector3 CameraFollowOffset => _cameraFollowOffset;

        public TimeSpan CameraShakeDuration => TimeSpan.FromSeconds(_cameraShakeDurationSeconds);
        public float CameraShakeAmplitude => _cameraShakeAmplitude;
        public float CameraShakeFrequency => _cameraShakeFrequency;
        public float CameraShakeDashKick => _cameraShakeDashKick;

        public TimeSpan SlashProjectileInterval => TimeSpan.FromSeconds(_slashProjectileIntervalSeconds);
        public float SlashProjectileSpeed => _slashProjectileSpeed;
        public TimeSpan SlashProjectileLifetime => TimeSpan.FromSeconds(_slashProjectileLifetimeSeconds);
        public float SlashProjectileScale => _slashProjectileScale;
        public float SlashProjectileSpawnDistance => _slashProjectileSpawnDistance;
        public int SlashProjectileDamage => _slashProjectileDamage;
        public bool SlashProjectilePierce => _slashProjectilePierce;
        public float SlashProjectileSpinSpeed => _slashProjectileSpinSpeed;
        public float SlashProjectileMaxSpinDegrees => _slashProjectileMaxSpinDegrees;

        public TimeSpan PlayerInvincibleDuration => TimeSpan.FromSeconds(_playerInvincibleDurationSeconds);
        public TimeSpan PlayerInvincibleBlinkInterval => TimeSpan.FromSeconds(_playerInvincibleBlinkIntervalSeconds);

        public float FlowGaugeMax => _flowGaugeMax;
        public float FlowGaugeGainOnHit => _flowGaugeGainOnHit;
        public float FlowGaugeLossOnDamaged => _flowGaugeLossOnDamaged;
        public TimeSpan FlowGaugeDecayDelay => TimeSpan.FromSeconds(_flowGaugeDecayDelaySeconds);
        public TimeSpan FlowGaugeDecayTickInterval => TimeSpan.FromSeconds(_flowGaugeDecayTickIntervalSeconds);
        public float FlowGaugeDecayAmountPerTick => _flowGaugeDecayAmountPerTick;

        public TimeSpan FlowDashInterval => TimeSpan.FromSeconds(_flowDashIntervalSeconds);
        public float MaxFlowDashDistance => _maxFlowDashDistance;
        public float FlowDashWallSkin => _flowDashWallSkin;
        public int FlowMaxDashCount => _flowMaxDashCount;
        public float FlowGravityScaleMultiplier => _flowGravityScaleMultiplier;
        public float FlowMonsterTimeScale => _flowMonsterTimeScale;

        public int FlowTrailDamage => _flowTrailDamage;
        public float FlowTrailHitWidth => _flowTrailHitWidth;
        public TimeSpan FlowTrailFadeDuration => TimeSpan.FromSeconds(_flowTrailFadeDurationSeconds);
        public float FlowTrailLineWidth => _flowTrailLineWidth;
        public Color FlowTrailColor => _flowTrailColor;

        public TimeSpan FlowPostProcessBlendDuration => TimeSpan.FromSeconds(_flowPostProcessBlendDurationSeconds);
        public float FlowGaugePreviewMaxWeight => _flowGaugePreviewMaxWeight;
        public TimeSpan FlowGaugePreviewBlendDuration => TimeSpan.FromSeconds(_flowGaugePreviewBlendDurationSeconds);
        public TimeSpan HpTrailHoldDuration => TimeSpan.FromSeconds(_hpTrailHoldSeconds);
        public float HpTrailMinSpeed => _hpTrailMinSpeed;
        public float HpTrailSpeedPerGap => _hpTrailSpeedPerGap;

        public float BgmMixerVolumeDb => _bgmMixerVolumeDb;
        public float SfxMixerVolumeDb => _sfxMixerVolumeDb;
        public float SfxFlowMixerVolumeDb => _sfxFlowMixerVolumeDb;
        public float SfxPitchVariation => _sfxPitchVariation;
        public float SfxVolume => _sfxVolume;
        public int SfxPoolSize => _sfxPoolSize;
        public float BgmVolume => _bgmVolume;
        public float BgmMuffledVolumeMultiplier => _bgmMuffledVolumeMultiplier;
        public float BgmMuffledCutoffFrequency => _bgmMuffledCutoffFrequency;
        public float BgmOpenCutoffFrequency => _bgmOpenCutoffFrequency;
        public float BgmMuffledResonanceQ => _bgmMuffledResonanceQ;
        public float BgmOpenResonanceQ => _bgmOpenResonanceQ;
        public TimeSpan BgmFlowBlendDuration => TimeSpan.FromSeconds(_bgmFlowBlendDurationSeconds);

        public float FlowPostExposure => _flowPostExposure;
        public float FlowContrast => _flowContrast;
        public Color FlowColorFilter => _flowColorFilter;
        public float FlowSaturation => _flowSaturation;
        public float FlowWhiteBalanceTemperature => _flowWhiteBalanceTemperature;
        public float FlowWhiteBalanceTint => _flowWhiteBalanceTint;
        public Color FlowShadowsColor => _flowShadowsColor;
        public float FlowShadowsOffset => _flowShadowsOffset;
        public Color FlowHighlightsColor => _flowHighlightsColor;
        public float FlowHighlightsOffset => _flowHighlightsOffset;
        public float FlowBloomIntensity => _flowBloomIntensity;
        public float FlowBloomThreshold => _flowBloomThreshold;
        public float FlowBloomScatter => _flowBloomScatter;
        public Color FlowBloomTint => _flowBloomTint;
        public float FlowVignetteIntensity => _flowVignetteIntensity;
        public float FlowVignetteSmoothness => _flowVignetteSmoothness;
        public Color FlowVignetteColor => _flowVignetteColor;
        public float FlowChromaticAberrationIntensity => _flowChromaticAberrationIntensity;
        public float FlowLensDistortionIntensity => _flowLensDistortionIntensity;
        public float FlowEnterPunchLensDistortion => _flowEnterPunchLensDistortion;
        public TimeSpan FlowEnterPunchDuration => TimeSpan.FromSeconds(_flowEnterPunchDurationSeconds);
        public float FlowFilmGrainIntensity => _flowFilmGrainIntensity;

        public float FlowBlurStrength => _flowBlurStrength;
        public float FlowBlurInnerRadius => _flowBlurInnerRadius;
        public float FlowBlurFalloff => _flowBlurFalloff;
        public float FlowLineCount => _flowLineCount;
        public float FlowLineDensity => _flowLineDensity;
        public float FlowLineWidth => _flowLineWidth;
        public float FlowLineInnerRadius => _flowLineInnerRadius;
        public float FlowLineFalloff => _flowLineFalloff;
        public float FlowLineOpacity => _flowLineOpacity;
        public Color FlowLineColor => _flowLineColor;
        public float FlowLineFlickerRate => _flowLineFlickerRate;
        public float FlowDashBurstIntensity => _flowDashBurstIntensity;
        public TimeSpan FlowDashBurstDuration => TimeSpan.FromSeconds(_flowDashBurstDurationSeconds);

        public TimeSpan MonsterSpawnDelay => TimeSpan.FromSeconds(_monsterSpawnDelaySeconds);
        public int MonsterMaxHp => _monsterMaxHp;
        public float MonsterMoveSpeed => _monsterMoveSpeed;
        public TimeSpan MonsterDamageBlinkDuration => TimeSpan.FromSeconds(_monsterDamageBlinkDurationSeconds);
        public Color MonsterDamageBlinkColor => _monsterDamageBlinkColor;
    }
}
