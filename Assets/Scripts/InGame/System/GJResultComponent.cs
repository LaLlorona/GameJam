using System;
using TMPro;
using UnityEngine;

namespace GameJam.InGame.System
{
    // View of the victory window (screen space uGUI). Hidden until GJGameResultManager shows it.
    public class GJResultComponent : MonoBehaviour
    {
        // Root of the window. Deactivated at start.
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _damagedCountText;
        [SerializeField] private TextMeshProUGUI _clearTimeText;
        [SerializeField] private TextMeshProUGUI _flowEnterCountText;

        public void Hide()
        {
            _root.SetActive(false);
        }

        public void ShowVictory(int damagedCount, TimeSpan clearTime, int flowEnterCount)
        {
            _damagedCountText.text = $"Times Hit: {damagedCount}";
            _clearTimeText.text = $"Clear Time: {GJGameRecord.FormatTime(clearTime)}";
            _flowEnterCountText.text = $"Flow Entered: {flowEnterCount}";
            _root.SetActive(true);
        }
    }
}
