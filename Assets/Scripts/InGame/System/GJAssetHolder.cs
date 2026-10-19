using System.Collections.Generic;
using GameJam.InGame.Monster;
using GameJam.InGame.Player;
using LDtkUnity;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

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
        [SerializeField] private GJResultComponent _resultComponent;
        // Optional. When empty, GJEffectManager builds a plain line instead.
        [SerializeField] private LineRenderer _flowTrailPrefab;
        // Global volume used only while the player is in Flow. Its weight is driven by GJPostProcessManager.
        [SerializeField] private Volume _flowVolume;
        // Looping background music. Muffled outside Flow and clear inside Flow (see GJSoundManager).
        [SerializeField] private AudioClip _bgmClip;
        // Optional. Needs groups "Master/BGM" and "Master/SFX" and exposed parameters "BGMVolume" / "SFXVolume".
        [SerializeField] private AudioMixer _audioMixer;
        // One clip is picked at random on every attack.
        [SerializeField] private AudioClip[] _basicAttackSfxClips;
        [SerializeField] private AudioClip[] _flowAttackSfxClips;
        public GJPlayerComponent PlayerComponent => _playerComponent;
        public CinemachineCamera InGameVirtualCamera => _inGameVirtualCamera;
        public LDtkComponentProject LDtkMap => _ldtkMap;
        public GJPlayerProjectileComponent SlashProjectilePrefab => _slashProjectilePrefab;
        public GJMonsterComponent MonsterPrefab => _monsterPrefab;
        public Transform MonsterSpawnPoint => _monsterSpawnPoint;
        public GJHUDComponent HUDComponent => _hudComponent;
        public GJResultComponent ResultComponent => _resultComponent;
        public LineRenderer FlowTrailPrefab => _flowTrailPrefab;
        public Volume FlowVolume => _flowVolume;
        public AudioClip BGMClip => _bgmClip;
        public AudioMixer AudioMixer => _audioMixer;
        public IReadOnlyList<AudioClip> BasicAttackSfxClips => _basicAttackSfxClips;
        public IReadOnlyList<AudioClip> FlowAttackSfxClips => _flowAttackSfxClips;
    }
}
