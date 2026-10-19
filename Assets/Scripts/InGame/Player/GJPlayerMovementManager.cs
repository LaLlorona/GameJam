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
        private GJInGameParameter _inGameParameter;
        private int _groundLayerMask;
        private int _remainingJumpCount;
        private bool _wasGrounded;
        private float _knockbackVelocityX;
        private float _knockbackVelocityY;
        private float _knockbackRemainingTime;
        public bool ISKnockbackActive => _knockbackRemainingTime > 0f;

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
            _inGameParameter = _parameter.InGameParameter;
            _groundLayerMask = LayerMask.GetMask(GJDefine.GROUND_LAYER_NAME);
            if (_groundLayerMask == 0)
            {
                Debug.LogError($"{GJDefine.GROUND_LAYER_NAME} no layers found .");
            }
            _playerComponent.SetGravityScale(_inGameParameter.PlayerGravityScale);
            _remainingJumpCount = _inGameParameter.PlayerMaxJumpCount;
            contextMap.GetRegisteredInstance<GJUpdateLoopManager>().RegisterUpdatable(this);
        }

        public GJUpdatePriority UpdatePriority => GJUpdatePriority.PlayerMovementUpdate;

        // Also runs in Flow state, so the player can move and jump between dash slashes.
        public void GameUpdate(TimeSpan deltaTime)
        {
            // Applied every frame so gravity edits in the inspector take effect immediately.
            _playerComponent.SetGravityScale(_inGameParameter.PlayerGravityScale);

            var velocity = _playerComponent.Velocity;
            UpdateJumpCount(velocity);

            if (ISKnockbackActive)
            {
                _knockbackRemainingTime -= (float)deltaTime.TotalSeconds;
                velocity.x = _knockbackVelocityX;
                // Don't override upward velocity from a jump.
                velocity.y = Mathf.Max(velocity.y, _knockbackVelocityY); 
                _playerComponent.SetVelocity(velocity);
                return;
            }

            var horizontalAxis = _inputManager.HorizontalAxis;
            velocity.x = horizontalAxis * _inGameParameter.PlayerMoveSpeed;
            if (horizontalAxis != 0f)
            {
                _playerComponent.SetFacing(horizontalAxis > 0f);
            }

            if (_inputManager.IsJumpPressedThisFrame && _remainingJumpCount > 0)
            {
                velocity.y = _inGameParameter.PlayerJumpVelocity;
                _remainingJumpCount--;
            }

            _playerComponent.SetVelocity(velocity);
        }

        public void ApplyKnockback(Vector2 direction, float force, float duration)
        {
            if(direction.sqrMagnitude > 0f)
            {
                direction.Normalize();
            }
            _knockbackVelocityX = direction.x * force;
            _knockbackVelocityY = direction.y * force; 
            _knockbackRemainingTime = duration;
        }

        private void UpdateJumpCount(Vector2 velocity)
        {
            var maxJumpCount = _inGameParameter.PlayerMaxJumpCount;
            // Clamp in case the max jump count was lowered in the inspector during play.
            _remainingJumpCount = Mathf.Min(_remainingJumpCount, maxJumpCount);
            IsGrounded = CheckGrounded();
            // Reset only while not rising, so the count isn't refilled on the frame right after a jump while still touching the ground.
            if (IsGrounded && velocity.y <= 0f)
            {
                _remainingJumpCount = maxJumpCount;
            }
            // Walking off a ledge without jumping consumes the ground jump.
            else if (_wasGrounded && IsGrounded == false && _remainingJumpCount == maxJumpCount)
            {
                _remainingJumpCount--;
            }
            _wasGrounded = IsGrounded;
        }

        private bool CheckGrounded()
        {
            var bounds = _playerComponent.ColliderBounds;
            var checkSize = new Vector2(bounds.size.x * _inGameParameter.GroundCheckWidthRatio, bounds.size.y);
            var hit = Physics2D.BoxCast(bounds.center, checkSize, 0f, Vector2.down, _inGameParameter.GroundCheckDistance, _groundLayerMask);
            return hit.collider != null;
        }
    }
}
