using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameJam.InGame.System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameJam.InGame.Effect
{
    // Creates effects and updates their fades.
    public class GJEffectManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private readonly List<GJFlowTrail> _flowTrails = new();
        private GJInGameParameter _inGameParameter;
        private Material _defaultTrailMaterial;

        public GJEffectManager(GJController.GJControllerParameter parameter)
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
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.EffectUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            foreach (var trail in _flowTrails.ToArray())
            {
                trail.UpdateTrail(deltaTime);
                if (trail.IsFadeFinished)
                {
                    _flowTrails.Remove(trail);
                    trail.DestroyTrail();
                }
            }
        }

        public GJFlowTrail CreateFlowTrail(Vector2 startPosition, Vector2 endPosition)
        {
            var trail = new GJFlowTrail(
                lineRenderer: CreateTrailLineRenderer(),
                startPosition: startPosition,
                endPosition: endPosition,
                width: _inGameParameter.FlowTrailLineWidth,
                color: _inGameParameter.FlowTrailColor
            );
            _flowTrails.Add(trail);
            return trail;
        }

        public void FadeOutFlowTrail(GJFlowTrail trail)
        {
            trail.StartFadeOut(_inGameParameter.FlowTrailFadeDuration);
        }

        // Uses GJAssetHolder.FlowTrailPrefab when assigned; otherwise builds a plain line so the trail works without scene setup.
        private LineRenderer CreateTrailLineRenderer()
        {
            var trailPrefab = _parameter.AssetHolder.FlowTrailPrefab;
            if (trailPrefab != null)
            {
                return Object.Instantiate(trailPrefab);
            }

            var lineRenderer = new GameObject("FlowTrail").AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = GetDefaultTrailMaterial();
            lineRenderer.sortingOrder = GJDefine.FLOW_TRAIL_SORTING_ORDER;
            return lineRenderer;
        }

        private Material GetDefaultTrailMaterial()
        {
            if (_defaultTrailMaterial != null)
            {
                return _defaultTrailMaterial;
            }

            var shader = Shader.Find(GJDefine.FLOW_TRAIL_DEFAULT_SHADER_NAME);
            if (shader == null)
            {
                Debug.LogError($"Shader '{GJDefine.FLOW_TRAIL_DEFAULT_SHADER_NAME}' not found. Assign GJAssetHolder.FlowTrailPrefab instead.");
                return null;
            }
            _defaultTrailMaterial = new Material(shader);
            return _defaultTrailMaterial;
        }
    }
}
