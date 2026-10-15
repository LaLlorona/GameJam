using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameJam.InGame.System
{
    // Drives the Flow-only global volume ("berserk" look) and the full screen speed effect (radial blur + speed lines).
    // The volume weight blends 0 <-> 1 when the player enters / leaves Flow,
    // and every override value is written from GJInGameParameter each frame so inspector edits apply immediately.
    // The speed effect shader reads global shader properties, so its material asset is never modified at runtime.
    public class GJPostProcessManager : IGJContext, IGJPriorityUpdatable
    {
        private static readonly int FlowSpeedIntensityId = Shader.PropertyToID(GJDefine.FLOW_SPEED_INTENSITY_PROPERTY);
        private static readonly int FlowSpeedCenterId = Shader.PropertyToID(GJDefine.FLOW_SPEED_CENTER_PROPERTY);
        private static readonly int FlowBlurStrengthId = Shader.PropertyToID(GJDefine.FLOW_BLUR_STRENGTH_PROPERTY);
        private static readonly int FlowBlurInnerRadiusId = Shader.PropertyToID(GJDefine.FLOW_BLUR_INNER_RADIUS_PROPERTY);
        private static readonly int FlowBlurFalloffId = Shader.PropertyToID(GJDefine.FLOW_BLUR_FALLOFF_PROPERTY);
        private static readonly int FlowLineCountId = Shader.PropertyToID(GJDefine.FLOW_LINE_COUNT_PROPERTY);
        private static readonly int FlowLineDensityId = Shader.PropertyToID(GJDefine.FLOW_LINE_DENSITY_PROPERTY);
        private static readonly int FlowLineWidthId = Shader.PropertyToID(GJDefine.FLOW_LINE_WIDTH_PROPERTY);
        private static readonly int FlowLineInnerRadiusId = Shader.PropertyToID(GJDefine.FLOW_LINE_INNER_RADIUS_PROPERTY);
        private static readonly int FlowLineFalloffId = Shader.PropertyToID(GJDefine.FLOW_LINE_FALLOFF_PROPERTY);
        private static readonly int FlowLineOpacityId = Shader.PropertyToID(GJDefine.FLOW_LINE_OPACITY_PROPERTY);
        private static readonly int FlowLineColorId = Shader.PropertyToID(GJDefine.FLOW_LINE_COLOR_PROPERTY);
        private static readonly int FlowLineFlickerRateId = Shader.PropertyToID(GJDefine.FLOW_LINE_FLICKER_RATE_PROPERTY);

        private readonly GJController.GJControllerParameter _parameter;
        private GJInGameParameter _inGameParameter;
        private GJPlayerManager _playerManager;
        private GJFlowGauge _flowGauge;
        private Volume _flowVolume;

        private ColorAdjustments _colorAdjustments;
        private WhiteBalance _whiteBalance;
        private ShadowsMidtonesHighlights _shadowsMidtonesHighlights;
        private Bloom _bloom;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;
        private LensDistortion _lensDistortion;
        private FilmGrain _filmGrain;

        private bool _wasInFlow;
        // 0 -> 1 blend of the Flow state itself. Drives the speed effect.
        private float _flowBlend;
        // Partial Flow look shown while the gauge fills up before entering Flow.
        private float _gaugePreviewWeight;
        // Time left on the lens distortion punch played when entering Flow.
        private TimeSpan _enterPunchRemainingTime;
        // Time left on the speed effect burst played on every dash slash.
        private TimeSpan _dashBurstRemainingTime;
        private int _lastRemainingDashCount;
        private Transform _playerTransform;

        public GJPostProcessManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            // Global shader values outlive play mode in the editor, so make sure the speed effect starts and ends off.
            ResetSpeedEffect();
            Application.quitting += ResetSpeedEffect;

            _flowVolume = _parameter.AssetHolder.FlowVolume;
            if (_flowVolume == null)
            {
                Debug.LogError("GJAssetHolder has no flow volume assigned. The Flow post processing will not be shown.");
                return;
            }

            _inGameParameter = _parameter.InGameParameter;
            _playerManager = contextMap.GetRegisteredInstance<GJPlayerManager>();
            _flowGauge = contextMap.GetRegisteredInstance<GJFlowGauge>();
            _playerTransform = _parameter.AssetHolder.PlayerComponent.transform;

            _flowVolume.isGlobal = true;
            _flowVolume.priority = GJDefine.FLOW_VOLUME_PRIORITY;
            _flowVolume.weight = 0f;
            SetUpProfile();
            ApplyParameter();
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PostProcessUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            var isInFlow = _playerManager.IsInFlow;
            if (isInFlow && _wasInFlow == false)
            {
                _enterPunchRemainingTime = _inGameParameter.FlowEnterPunchDuration;
            }
            _wasInFlow = isInFlow;

            // The first dash slash fires on Flow enter, and every later one lowers the remaining count.
            if (_playerManager.TryGetRemainingFlowDashCount(out var remainingDashCount))
            {
                if (remainingDashCount != _lastRemainingDashCount)
                {
                    _dashBurstRemainingTime = _inGameParameter.FlowDashBurstDuration;
                }
                _lastRemainingDashCount = remainingDashCount;
            }
            else
            {
                _lastRemainingDashCount = -1;
            }

            UpdateWeight(isInFlow, deltaTime);

            _enterPunchRemainingTime = DecreaseTime(_enterPunchRemainingTime, deltaTime);
            _dashBurstRemainingTime = DecreaseTime(_dashBurstRemainingTime, deltaTime);

            ApplyParameter();
            ApplySpeedEffect();
        }

        private static TimeSpan DecreaseTime(TimeSpan remainingTime, TimeSpan deltaTime)
        {
            var result = remainingTime - deltaTime;
            return result < TimeSpan.Zero ? TimeSpan.Zero : result;
        }

        // Remaining ratio (1 -> 0) of a timer that started at duration.
        private static float GetRemainingRatio(TimeSpan remainingTime, TimeSpan duration)
        {
            var durationSeconds = (float)duration.TotalSeconds;
            return durationSeconds > 0f
                ? Mathf.Clamp01((float)remainingTime.TotalSeconds / durationSeconds)
                : 0f;
        }

        private void ApplySpeedEffect()
        {
            var parameter = _inGameParameter;
            // Follows the Flow blend (not the gauge preview) so the effect blends in and out with the color grading.
            // The dash burst only applies while in Flow.
            var burst = _wasInFlow
                ? parameter.FlowDashBurstIntensity * GetRemainingRatio(_dashBurstRemainingTime, parameter.FlowDashBurstDuration)
                : 0f;
            Shader.SetGlobalFloat(FlowSpeedIntensityId, _flowBlend + burst);

            var mainCamera = _parameter.MainCamera;
            if (mainCamera != null && _playerTransform != null)
            {
                Vector2 center = mainCamera.WorldToViewportPoint(_playerTransform.position);
                Shader.SetGlobalVector(FlowSpeedCenterId, center);
            }

            Shader.SetGlobalFloat(FlowBlurStrengthId, parameter.FlowBlurStrength);
            Shader.SetGlobalFloat(FlowBlurInnerRadiusId, parameter.FlowBlurInnerRadius);
            Shader.SetGlobalFloat(FlowBlurFalloffId, parameter.FlowBlurFalloff);
            Shader.SetGlobalFloat(FlowLineCountId, parameter.FlowLineCount);
            Shader.SetGlobalFloat(FlowLineDensityId, parameter.FlowLineDensity);
            Shader.SetGlobalFloat(FlowLineWidthId, parameter.FlowLineWidth);
            Shader.SetGlobalFloat(FlowLineInnerRadiusId, parameter.FlowLineInnerRadius);
            Shader.SetGlobalFloat(FlowLineFalloffId, parameter.FlowLineFalloff);
            Shader.SetGlobalFloat(FlowLineOpacityId, parameter.FlowLineOpacity);
            Shader.SetGlobalColor(FlowLineColorId, parameter.FlowLineColor);
            Shader.SetGlobalFloat(FlowLineFlickerRateId, parameter.FlowLineFlickerRate);
        }

        private static void ResetSpeedEffect()
        {
            Shader.SetGlobalFloat(FlowSpeedIntensityId, 0f);
        }

        // Volume weight = the larger of
        //  - Flow blend: 0 <-> 1 linearly over FlowPostProcessBlendDuration on enter / exit.
        //  - Gauge preview: FlowRatio * FlowGaugePreviewMaxWeight, so the screen slowly tints as the gauge fills.
        //    Smoothed so +/- gauge steps fade in instead of popping.
        private void UpdateWeight(bool isInFlow, TimeSpan deltaTime)
        {
            var parameter = _inGameParameter;
            var deltaSeconds = (float)deltaTime.TotalSeconds;

            _flowBlend = MoveTowardsOverDuration(_flowBlend, isInFlow ? 1f : 0f, deltaSeconds, parameter.FlowPostProcessBlendDuration, 1f);

            var previewMaxWeight = parameter.FlowGaugePreviewMaxWeight;
            var previewTarget = isInFlow ? 0f : _flowGauge.FlowRatio * previewMaxWeight;
            _gaugePreviewWeight = MoveTowardsOverDuration(_gaugePreviewWeight, previewTarget, deltaSeconds, parameter.FlowGaugePreviewBlendDuration, previewMaxWeight);

            _flowVolume.weight = Mathf.Max(_flowBlend, _gaugePreviewWeight);
        }

        // Moves toward target so that a change of fullRange takes exactly duration.
        private static float MoveTowardsOverDuration(float current, float target, float deltaSeconds, TimeSpan duration, float fullRange)
        {
            var durationSeconds = (float)duration.TotalSeconds;
            if (durationSeconds <= 0f || fullRange <= 0f)
            {
                return target;
            }
            return Mathf.MoveTowards(current, target, deltaSeconds / durationSeconds * fullRange);
        }

        // Uses a runtime copy of the profile so play mode never modifies the profile asset.
        // When no profile is assigned, an empty one is created. Missing overrides are added.
        private void SetUpProfile()
        {
            VolumeProfile profile;
            if (_flowVolume.sharedProfile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "GJFlowVolumeProfile (Runtime)";
                _flowVolume.sharedProfile = profile;
            }
            else
            {
                profile = _flowVolume.profile;
            }

            _colorAdjustments = GetOrAddOverride<ColorAdjustments>(profile);
            _whiteBalance = GetOrAddOverride<WhiteBalance>(profile);
            _shadowsMidtonesHighlights = GetOrAddOverride<ShadowsMidtonesHighlights>(profile);
            _bloom = GetOrAddOverride<Bloom>(profile);
            _vignette = GetOrAddOverride<Vignette>(profile);
            _chromaticAberration = GetOrAddOverride<ChromaticAberration>(profile);
            _lensDistortion = GetOrAddOverride<LensDistortion>(profile);
            _filmGrain = GetOrAddOverride<FilmGrain>(profile);
        }

        private static T GetOrAddOverride<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var component) == false)
            {
                component = profile.Add<T>();
            }
            component.active = true;
            return component;
        }

        private void ApplyParameter()
        {
            var parameter = _inGameParameter;

            SetOverride(_colorAdjustments.postExposure, parameter.FlowPostExposure);
            SetOverride(_colorAdjustments.contrast, parameter.FlowContrast);
            SetOverride(_colorAdjustments.colorFilter, parameter.FlowColorFilter);
            SetOverride(_colorAdjustments.saturation, parameter.FlowSaturation);

            SetOverride(_whiteBalance.temperature, parameter.FlowWhiteBalanceTemperature);
            SetOverride(_whiteBalance.tint, parameter.FlowWhiteBalanceTint);

            SetOverride(_shadowsMidtonesHighlights.shadows, ToToneVector(parameter.FlowShadowsColor, parameter.FlowShadowsOffset));
            SetOverride(_shadowsMidtonesHighlights.highlights, ToToneVector(parameter.FlowHighlightsColor, parameter.FlowHighlightsOffset));

            SetOverride(_bloom.intensity, parameter.FlowBloomIntensity);
            SetOverride(_bloom.threshold, parameter.FlowBloomThreshold);
            SetOverride(_bloom.scatter, parameter.FlowBloomScatter);
            SetOverride(_bloom.tint, parameter.FlowBloomTint);

            SetOverride(_vignette.intensity, parameter.FlowVignetteIntensity);
            SetOverride(_vignette.smoothness, parameter.FlowVignetteSmoothness);
            SetOverride(_vignette.color, parameter.FlowVignetteColor);

            SetOverride(_chromaticAberration.intensity, parameter.FlowChromaticAberrationIntensity);

            SetOverride(_lensDistortion.intensity, GetLensDistortionIntensity());

            SetOverride(_filmGrain.intensity, parameter.FlowFilmGrainIntensity);
        }

        // Base distortion plus a punch that fades out right after entering Flow.
        private float GetLensDistortionIntensity()
        {
            var parameter = _inGameParameter;
            var punchRatio = GetRemainingRatio(_enterPunchRemainingTime, parameter.FlowEnterPunchDuration);
            var intensity = parameter.FlowLensDistortionIntensity + parameter.FlowEnterPunchLensDistortion * punchRatio;
            return Mathf.Clamp(intensity, -1f, 1f);
        }

        // ShadowsMidtonesHighlights stores rgb as the tint and w as the brightness offset.
        private static Vector4 ToToneVector(Color color, float offset)
        {
            return new Vector4(color.r, color.g, color.b, offset);
        }

        private static void SetOverride<T>(VolumeParameter<T> volumeParameter, T value)
        {
            volumeParameter.overrideState = true;
            volumeParameter.value = value;
        }
    }
}
