using System;
using Cysharp.Threading.Tasks;
using GameJam.InGame.System;
using UnityEngine;

namespace GameJam.InGame.Player
{
    public class GJPlayerMovementManager : IGJContext, IGJPriorityUpdatable
    {
        private readonly GJController.GJControllerParameter _parameter;
        private GJInputManager _inputManager;
        private GJPlayerComponent _playerComponent;
        private int _groundLayerMask;
        private int _remainingJumpCount;
        private bool _wasGrounded;
        private bool _isMovementEnabled = true;

        public bool IsGrounded { get; private set; }

        public GJPlayerMovementManager(GJController.GJControllerParameter parameter)
        {
            _parameter = parameter;
        }

        public UniTask InitAsync()
        {
            return UniTask.CompletedTask;
        }

        public void WireContextAfterInit(GJContextMap contextMap)
        {
            _inputManager = contextMap.GetRegisteredInstance<GJInputManager>();
            _playerComponent = _parameter.AssetHolder.PlayerComponent;
            _groundLayerMask = LayerMask.GetMask(GJDefine.GROUND_LAYER_NAME);
            if (_groundLayerMask == 0)
            {
                Debug.LogError($"{GJDefine.GROUND_LAYER_NAME} no layers found .");
            }
            _playerComponent.SetGravityScale(GJDefine.PLAYER_GRAVITY_SCALE);
            _remainingJumpCount = GJDefine.PLAYER_MAX_JUMP_COUNT;
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        // In Flow state the dash slash replaces movement, so movement input is blocked.
        public void SetMovementEnabled(bool isEnabled)
        {
            _isMovementEnabled = isEnabled;
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PlayerMovementUpdate;

        public void GameUpdate(TimeSpan deltaTime)
        {
            if (_isMovementEnabled == false)
            {
                return;
            }

            var velocity = _playerComponent.Velocity;
            UpdateJumpCount(velocity);

            var horizontalAxis = _inputManager.HorizontalAxis;
            velocity.x = horizontalAxis * GJDefine.PLAYER_MOVE_SPEED;
            if (horizontalAxis != 0f)
            {
                _playerComponent.SetFacing(horizontalAxis > 0f);
            }

            if (_inputManager.IsJumpPressedThisFrame && _remainingJumpCount > 0)
            {
                velocity.y = GJDefine.PLAYER_JUMP_VELOCITY;
                _remainingJumpCount--;
            }

            _playerComponent.SetVelocity(velocity);
        }

        private void UpdateJumpCount(Vector2 velocity)
        {
            IsGrounded = CheckGrounded();
            // Reset only while not rising, so the count isn't refilled on the frame right after a jump while still touching the ground.
            if (IsGrounded && velocity.y <= 0f)
            {
                _remainingJumpCount = GJDefine.PLAYER_MAX_JUMP_COUNT;
            }
            // Walking off a ledge without jumping consumes the ground jump.
            else if (_wasGrounded && IsGrounded == false && _remainingJumpCount == GJDefine.PLAYER_MAX_JUMP_COUNT)
            {
                _remainingJumpCount--;
            }
            _wasGrounded = IsGrounded;
        }

        private bool CheckGrounded()
        {
            var bounds = _playerComponent.ColliderBounds;
            var checkSize = new Vector2(bounds.size.x * GJDefine.GROUND_CHECK_WIDTH_RATIO, bounds.size.y);
            var hit = Physics2D.BoxCast(bounds.center, checkSize, 0f, Vector2.down, GJDefine.GROUND_CHECK_DISTANCE, _groundLayerMask);
            return hit.collider != null;
        }
    }
}
