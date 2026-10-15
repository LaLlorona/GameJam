using GameJam.InGame.Monster;
using GameJam.InGame.Player;
using LDtkUnity;
using Unity.Cinemachine;
using UnityEngine;

namespace GameJam.InGame.System
{
    public class GJAssetHolder : MonoBehaviour
    {
        [SerializeField] private GJPlayerComponent _playerComponent;
        [SerializeField] private CinemachineCamera _inGameVirtualCamera;
        [SerializeField] private LDtkComponentProject _ldtkMap;
        [SerializeField] private GJPlayerProjectileComponent _slashProjectilePrefab;
        [SerializeField] private GJMonsterComponent _monsterPrefab;
        [SerializeField] private Transform _monsterSpawnPoint;
        [SerializeField] private GJHUDComponent _hudComponent;
        // Optional. When empty, GJEffectManager builds a plain line instead.
        [SerializeField] private LineRenderer _flowTrailPrefab;
        public GJPlayerComponent PlayerComponent => _playerComponent;
        public CinemachineCamera InGameVirtualCamera => _inGameVirtualCamera;
        public LDtkComponentProject LDtkMap => _ldtkMap;
        public GJPlayerProjectileComponent SlashProjectilePrefab => _slashProjectilePrefab;
        public GJMonsterComponent MonsterPrefab => _monsterPrefab;
        public Transform MonsterSpawnPoint => _monsterSpawnPoint;
        public GJHUDComponent HUDComponent => _hudComponent;
        public LineRenderer FlowTrailPrefab => _flowTrailPrefab;
    }
}
