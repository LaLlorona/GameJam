using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.Player;
using UnityEngine;

namespace GameJam.InGame.System
{
    public class GJController
    {
        public record GJControllerParameter(
            Camera MainCamera,
            GJAssetHolder AssetHolder
        );

        private readonly GJContextMap _contextMap;
        private readonly GJUpdateLoopManager _updateLoopManager;

        public GJController(GJControllerParameter controllerParameter)
        {
            _updateLoopManager = new GJUpdateLoopManager();
            _contextMap = new GJContextMap();
            _contextMap.Register(_updateLoopManager);
            _contextMap.Register(new GJGameStateManager());
            _contextMap.Register(new GJInputManager(controllerParameter));
            _contextMap.Register(new GJPlayerMovementManager(controllerParameter));
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
