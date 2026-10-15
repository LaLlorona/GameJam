using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.System;
using UnityEngine;

namespace GameJam.InGame.Player
{
    // Flow gauge of the player (0 ~ FlowGaugeMax).
    // Fills when the player damages monsters, decays over time after the last hit, and is locked while in Flow.
    public class GJFlowGauge : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private GJInGameParameter _inGameParameter;
        private TimeSpan _elapsedTimeFromLastHit = TimeSpan.Zero;
        private TimeSpan _elapsedTimeFromLastDecayTick = TimeSpan.Zero;
        private bool _isLocked;

        public float CurrentFlow { get; private set; }
        public float MaxFlow => _inGameParameter.FlowGaugeMax;
        public float FlowRatio => MaxFlow > 0f ? Mathf.Clamp01(CurrentFlow / MaxFlow) : 0f;
        public bool IsFull => CurrentFlow >= MaxFlow;

        public GJFlowGauge(GJController.GJControllerParameter parameter)
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

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.FlowGaugeUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            // Clamp in case the max was lowered in the inspector during play.
            CurrentFlow = Mathf.Min(CurrentFlow, MaxFlow);
            if (_isLocked || CurrentFlow <= 0f)
            {
                return;
            }

            _elapsedTimeFromLastHit += deltaTime;
            if (_elapsedTimeFromLastHit < _inGameParameter.FlowGaugeDecayDelay)
            {
                return;
            }

            _elapsedTimeFromLastDecayTick += deltaTime;
            var tickInterval = _inGameParameter.FlowGaugeDecayTickInterval;
            if (_elapsedTimeFromLastDecayTick < tickInterval)
            {
                return;
            }

            _elapsedTimeFromLastDecayTick -= tickInterval;
            CurrentFlow = Mathf.Max(CurrentFlow - _inGameParameter.FlowGaugeDecayAmountPerTick, 0f);
        }

        // Called once for every monster the player damages.
        public void AddFlowOnHit()
        {
            if (_isLocked)
            {
                return;
            }

            CurrentFlow = Mathf.Min(CurrentFlow + _inGameParameter.FlowGaugeGainOnHit, MaxFlow);
            _elapsedTimeFromLastHit = TimeSpan.Zero;
            _elapsedTimeFromLastDecayTick = TimeSpan.Zero;
        }

        // Called when the player is hit by a monster.
        public void LoseFlowOnDamaged()
        {
            if (_isLocked)
            {
                return;
            }

            CurrentFlow = Mathf.Max(CurrentFlow - _inGameParameter.FlowGaugeLossOnDamaged, 0f);
        }

        // While locked (Flow state) the gauge neither gains nor decays.
        public void SetLocked(bool isLocked)
        {
            _isLocked = isLocked;
        }

        public void ResetGauge()
        {
            CurrentFlow = 0f;
            _elapsedTimeFromLastHit = TimeSpan.Zero;
            _elapsedTimeFromLastDecayTick = TimeSpan.Zero;
        }
    }
}
