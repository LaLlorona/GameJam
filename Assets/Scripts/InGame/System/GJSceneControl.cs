using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameJam.InGame.System
{
    public class GJSceneControl : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private GJUpdateLoopProvider _updateLoopProvider;
        [SerializeField] private GJAssetHolder _assetHolder;

        private GJController _gameController;

        private void Start()
        {
            InitSceneControl().Forget();
        }

        private async UniTask InitSceneControl()
        {
            var controllerParameter = new GJController.GJControllerParameter(
                MainCamera: _mainCamera,
                AssetHolder: _assetHolder
            );
            _gameController = new GJController(controllerParameter);
            await _gameController.InitializeGameController();
            _updateLoopProvider.Register(_gameController);
        }
    }
}
