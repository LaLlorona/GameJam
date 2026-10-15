using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameJam.InGame.System
{
    // View of the in-game HUD (screen space uGUI). GJHUDManager pushes values into it every frame.
    public class GJHUDComponent : MonoBehaviour
    {
        [Header("Monster HP")]
        // Root of the monster HP bar. Hidden while no monster is alive.
        [SerializeField] private GameObject _monsterHpRoot;
        // Image Type must be Filled (Horizontal) so fillAmount works.
        [SerializeField] private Image _monsterHpFillImage;
        // Optional "current / max" label.
        [SerializeField] private TextMeshProUGUI _monsterHpText;

        [Header("Flow Gauge")]
        // Image Type must be Filled (Horizontal) so fillAmount works. Optional.
        [SerializeField] private Image _flowGaugeFillImage;
        // Shows the remaining dash slash count while in Flow. Optional.
        [SerializeField] private TextMeshProUGUI _flowDashCountText;

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

        public void SetMonsterHpVisible(bool isVisible)
        {
            if (_monsterHpRoot.activeSelf != isVisible)
            {
                _monsterHpRoot.SetActive(isVisible);
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
