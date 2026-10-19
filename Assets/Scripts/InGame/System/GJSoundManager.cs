using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Player;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace GameJam.InGame.System
{
    // Plays the BGM and the one-shot sound effects, each routed to its own audio mixer group.
    //  - BGM: a looping source. Outside Flow it is low pass filtered ("listening underwater"),
    //    and the filter opens smoothly when the player enters Flow and closes again when Flow ends.
    //  - SFX: a small pool of sources so sounds can overlap, e.g. Flow dash slashes that fire every 0.3 seconds.
    // Every value is read from GJInGameParameter each frame so inspector edits apply immediately.
    public class GJSoundManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private readonly List<AudioSource> _sfxAudioSources = new();
        private readonly Dictionary<IReadOnlyList<AudioClip>, int> _lastPlayedSfxIndices = new();
        private GJInGameParameter _inGameParameter;
        private GJPlayerManager _playerManager;
        private AudioMixer _audioMixer;
        private AudioSource _bgmAudioSource;
        private AudioLowPassFilter _bgmLowPassFilter;
        private int _nextSfxSourceIndex;

        // 0 = muffled (normal), 1 = clear (Flow).
        private float _flowBlend;

        public GJSoundManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _inGameParameter = _parameter.InGameParameter;
            _playerManager = contextMap.GetRegisteredInstance<GJPlayerManager>();
            _audioMixer = _parameter.AssetHolder.AudioMixer;
            if (_audioMixer == null)
            {
                Debug.LogWarning("GJAssetHolder has no audio mixer assigned. The sounds will not be routed to mixer groups.");
            }

            var rootObject = new GameObject("GJSound");
            SetUpBgm(rootObject);
            SetUpSfx(rootObject);

            ApplyParameter();
            if (_bgmAudioSource != null)
            {
                _bgmAudioSource.Play();
            }
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.SoundUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            var durationSeconds = (float)_inGameParameter.BgmFlowBlendDuration.TotalSeconds;
            var target = _playerManager.IsInFlow ? 1f : 0f;
            _flowBlend = durationSeconds > 0f
                ? Mathf.MoveTowards(_flowBlend, target, (float)deltaTime.TotalSeconds / durationSeconds)
                : target;

            ApplyParameter();
        }

        // Plays a random clip of the list. The same clip is not picked twice in a row when the list has two or more clips.
        public void PlaySfxRandom(IReadOnlyList<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0)
            {
                return;
            }

            var lastIndex = _lastPlayedSfxIndices.TryGetValue(clips, out var storedIndex) ? storedIndex : -1;
            int index;
            if (clips.Count == 1)
            {
                index = 0;
            }
            else
            {
                // Pick from the other clips by skipping over the last played index.
                index = Random.Range(0, clips.Count - 1);
                if (index >= lastIndex && lastIndex >= 0)
                {
                    index++;
                }
            }
            _lastPlayedSfxIndices[clips] = index;

            var clip = clips[index];
            if (clip == null)
            {
                return;
            }

            // Round robin, so the oldest sound is cut off first when every source is busy.
            var audioSource = _sfxAudioSources[_nextSfxSourceIndex];
            _nextSfxSourceIndex = (_nextSfxSourceIndex + 1) % _sfxAudioSources.Count;

            var pitchVariation = _inGameParameter.SfxPitchVariation;
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.volume = _inGameParameter.SfxVolume;
            audioSource.clip = clip;
            audioSource.Play();
        }

        private void SetUpBgm(GameObject rootObject)
        {
            var bgmClip = _parameter.AssetHolder.BGMClip;
            if (bgmClip == null)
            {
                Debug.LogError("GJAssetHolder has no BGM clip assigned. The BGM will not be played.");
                return;
            }

            // The low pass filter must be added before the AudioSource starts playing to be applied from the first sample.
            var bgmObject = new GameObject("GJBGM");
            bgmObject.transform.SetParent(rootObject.transform);
            _bgmAudioSource = bgmObject.AddComponent<AudioSource>();
            _bgmLowPassFilter = bgmObject.AddComponent<AudioLowPassFilter>();
            _bgmAudioSource.clip = bgmClip;
            _bgmAudioSource.loop = true;
            _bgmAudioSource.playOnAwake = false;
            _bgmAudioSource.spatialBlend = 0f;
            _bgmAudioSource.outputAudioMixerGroup = FindMixerGroup(GJDefine.BGM_MIXER_GROUP_PATH);
        }

        private void SetUpSfx(GameObject rootObject)
        {
            var sfxGroup = FindMixerGroup(GJDefine.SFX_MIXER_GROUP_PATH);
            var sfxObject = new GameObject("GJSFX");
            sfxObject.transform.SetParent(rootObject.transform);
            for (var i = 0; i < _inGameParameter.SfxPoolSize; i++)
            {
                var audioSource = sfxObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
                audioSource.outputAudioMixerGroup = sfxGroup;
                _sfxAudioSources.Add(audioSource);
            }
        }

        // Returns null when there is no mixer or no such group, which leaves the source on the default output.
        private AudioMixerGroup FindMixerGroup(string groupPath)
        {
            if (_audioMixer == null)
            {
                return null;
            }

            var groups = _audioMixer.FindMatchingGroups(groupPath);
            if (groups.Length == 0)
            {
                Debug.LogError($"Audio mixer has no group '{groupPath}'.");
                return null;
            }
            return groups[0];
        }

        private void ApplyParameter()
        {
            var parameter = _inGameParameter;

            if (_bgmAudioSource != null)
            {
                // Interpolate the cutoff in log space because pitch perception is logarithmic.
                // A linear lerp would sound like nothing happens until the very end.
                var mutedCutoff = parameter.BgmMuffledCutoffFrequency;
                var openCutoff = parameter.BgmOpenCutoffFrequency;
                _bgmLowPassFilter.cutoffFrequency = mutedCutoff * Mathf.Pow(openCutoff / mutedCutoff, _flowBlend);
                _bgmLowPassFilter.lowpassResonanceQ = Mathf.Lerp(parameter.BgmMuffledResonanceQ, parameter.BgmOpenResonanceQ, _flowBlend);

                _bgmAudioSource.volume = parameter.BgmVolume * Mathf.Lerp(parameter.BgmMuffledVolumeMultiplier, 1f, _flowBlend);
            }

            if (_audioMixer != null)
            {
                _audioMixer.SetFloat(GJDefine.BGM_VOLUME_MIXER_PARAMETER, parameter.BgmMixerVolumeDb);
                var sfxVolumeDb = Mathf.Lerp(parameter.SfxMixerVolumeDb, parameter.SfxFlowMixerVolumeDb, _flowBlend);
                _audioMixer.SetFloat(GJDefine.SFX_VOLUME_MIXER_PARAMETER, sfxVolumeDb);
            }
        }
    }
}
