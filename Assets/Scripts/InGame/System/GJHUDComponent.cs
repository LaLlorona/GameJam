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

        [Header("Flow Gauge")]
        // Image Type must be Filled (Horizontal) so fillAmount works. Optional.
        [SerializeField] private Image _flowGaugeFillImage;
        // Shows the remaining dash slash count while in Flow. Optional.
        [SerializeField] private TextMeshProUGUI _flowDashCountText;

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

        public void SetMonsterHp(int currentHp, int maxHp)
        {
            _monsterHpFillImage.fillAmount = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;
            if (_monsterHpText != null)
            {
                _monsterHpText.text = $"{currentHp} / {maxHp}";
            }
        }
    }
}
