using GameJam.InGame.Player;
using UnityEngine;

namespace GameJam.InGame.System
{
    public class GJAssetHolder : MonoBehaviour
    {
        [SerializeField] private GJPlayerComponent _playerComponent;
        public GJPlayerComponent PlayerComponent => _playerComponent;
    }
}
