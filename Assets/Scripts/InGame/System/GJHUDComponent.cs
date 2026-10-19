using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.InGame.System
{
    // View of the in-game HUD (screen space uGUI). GJHUDManager pushes values into it every frame.
    public class GJHUDComponent : MonoBehaviour
    {
        // Parent of every HUD element. Hidden until the monster spawns.
        // Keep the result screen outside of it so the result screen is not hidden together.
        [SerializeField] private GameObject _hudRoot;

        [Header("Timer")]
        // Elapsed time since the game started, shown at the top left.
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Monster HP")]
        // Image Type must be Filled (Horizontal) so fillAmount works.
        [SerializeField] private Image _monsterHpFillImage;
        // Optional "current / max" label.
        [SerializeField] private TextMeshProUGUI _monsterHpText;
        // Optional white bar behind the fill. It stays at the old HP after damage and then catches up, which shows burst damage.
        // Image Type must be Filled (Horizontal) and it must be drawn behind the fill image.
        [SerializeField] private Image _monsterHpTrailImage;

        [Header("Flow Gauge")]
        // Image Type must be Filled (Horizontal) so fillAmount works. Optional.
        [SerializeField] private Image _flowGaugeFillImage;
        // Shows the remaining dash slash count while in Flow. Optional.
        [SerializeField] private TextMeshProUGUI _flowDashCountText;

        private float _monsterHpTrailRatio = 1f;
        private bool _isMonsterHpTrailWaiting;
        private float _monsterHpTrailHoldRemainingSeconds;

        public void SetHudVisible(bool isVisible)
        {
            if (_hudRoot.activeSelf != isVisible)
            {
                _hudRoot.SetActive(isVisible);
            }
        }

        public void SetElapsedTime(TimeSpan elapsedTime)
        {
            if (_timerText != null)
            {
                _timerText.text = GJGameRecord.FormatTime(elapsedTime);
            }
        }

        public void SetFlowGauge(float flowRatio)
        {
            if (_flowGaugeFillImage != null)
            {
                _flowGaugeFillImage.fillAmount = Mathf.Clamp01(flowRatio);
            }
        }

        public void SetFlowDashCount(bool isVisible, int remainingDashCount)
        {
            if (_flowDashCountText == null)
            {
                return;
            }

            if (_flowDashCountText.gameObject.activeSelf != isVisible)
            {
                _flowDashCountText.gameObject.SetActive(isVisible);
            }
            if (isVisible)
            {
                _flowDashCountText.text = remainingDashCount.ToString();
            }
        }

        public void SetMonsterHp(int currentHp, int maxHp, TimeSpan deltaTime, GJInGameParameter inGameParameter)
        {
            var hpRatio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            _monsterHpFillImage.fillAmount = hpRatio;
            if (_monsterHpText != null)
            {
                _monsterHpText.text = $"{currentHp} / {maxHp}";
            }
            UpdateMonsterHpTrail(hpRatio, (float)deltaTime.TotalSeconds, inGameParameter);
        }

        // The trail waits for HpTrailHoldDuration after the first damage and then drains toward the fill.
        // The wait is not extended by later hits, so the trail starts moving even under constant damage.
        // The drain speed grows with the gap, so a big burst drains fast and the trail never falls far behind.
        private void UpdateMonsterHpTrail(float hpRatio, float deltaSeconds, GJInGameParameter inGameParameter)
        {
            if (hpRatio >= _monsterHpTrailRatio)
            {
                // Healed or at full HP: nothing to show.
                _monsterHpTrailRatio = hpRatio;
                _isMonsterHpTrailWaiting = false;
            }
            else
            {
                if (_isMonsterHpTrailWaiting == false)
                {
                    _isMonsterHpTrailWaiting = true;
                    _monsterHpTrailHoldRemainingSeconds = (float)inGameParameter.HpTrailHoldDuration.TotalSeconds;
                }

                if (_monsterHpTrailHoldRemainingSeconds > 0f)
                {
                    _monsterHpTrailHoldRemainingSeconds -= deltaSeconds;
                }
                else
                {
                    var gap = _monsterHpTrailRatio - hpRatio;
                    var speed = Mathf.Max(inGameParameter.HpTrailMinSpeed, gap * inGameParameter.HpTrailSpeedPerGap);
                    _monsterHpTrailRatio = Mathf.Max(hpRatio, _monsterHpTrailRatio - speed * deltaSeconds);
                    if (_monsterHpTrailRatio <= hpRatio)
                    {
                        _isMonsterHpTrailWaiting = false;
                    }
                }
            }

            if (_monsterHpTrailImage != null)
            {
                _monsterHpTrailImage.fillAmount = _monsterHpTrailRatio;
            }
        }
    }
}
