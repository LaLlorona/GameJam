using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Effect;
using GameJam.InGame.Monster;
using GameJam.InGame.Player;
using UnityEngine;

namespace GameJam.InGame.System
{
    public class GJController
    {
        public record GJControllerParameter(
            Camera MainCamera,
            GJAssetHolder AssetHolder,
            GJInGameParameter InGameParameter
        );

        private readonly GJContextMap _contextMap;
        private readonly GJUpdateLoopManager _updateLoopManager;

        public GJController(GJControllerParameter controllerParameter)
        {
            if (controllerParameter.InGameParameter == null)
            {
                throw new InvalidOperationException("GJSceneControl has no GJInGameParameter assigned.");
            }

            _updateLoopManager = new GJUpdateLoopManager();
            _contextMap = new GJContextMap();
            _contextMap.Register(_updateLoopManager);
            _contextMap.Register(new GJGameStateManager());
            _contextMap.Register(new GJGameRecord());
            _contextMap.Register(new GJGameResultManager(controllerParameter));
            _contextMap.Register(new GJInputManager(controllerParameter));
            _contextMap.Register(new GJMapManager(controllerParameter));
            _contextMap.Register(new GJPlayerMovementManager(controllerParameter));
            _contextMap.Register(new GJPlayerManager(controllerParameter));
            _contextMap.Register(new GJPlayerHitManager(controllerParameter));
            _contextMap.Register(new GJFlowGauge(controllerParameter));
            _contextMap.Register(new GJPlayerProjectileManager(controllerParameter));
            _contextMap.Register(new GJMonsterManager(controllerParameter));
            _contextMap.Register(new GJEffectManager(controllerParameter));
            _contextMap.Register(new GJPostProcessManager(controllerParameter));
            _contextMap.Register(new GJHUDManager(controllerParameter));
            _contextMap.Register(new GJCameraManager(controllerParameter));
        }

        public async UniTask InitializeGameController()
        {
            await _contextMap.InitializeAllContextAsync();
            _contextMap.WireContextDependencyAfterInit();
            var gameStateManager = _contextMap.GetRegisteredInstance<GJGameStateManager>();
            gameStateManager.ResumeGame();
        }

        public void OnUpdate(TimeSpan deltaTime)
        {
            _updateLoopManager.UpdateInGameLoop(deltaTime);
        }
    }
}
