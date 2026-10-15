using UnityEngine;

namespace GameJam.InGame.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class GJPlayerComponent : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private BoxCollider2D _boxCollider2D;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public Vector2 Position => _rigidbody2D.position;
        public Vector2 Velocity => _rigidbody2D.linearVelocity;
        public Collider2D Collider => _boxCollider2D;
        public Bounds ColliderBounds => _boxCollider2D.bounds;
        public Transform CameraFollowTransform => transform;
        public bool IsFacingRight => _spriteRenderer.flipX == false;

        public void SetVelocity(Vector2 velocity)
        {
            _rigidbody2D.linearVelocity = velocity;
        }

        // Moves the body instantly (no sweep) and stops it.
        public void Teleport(Vector2 position)
        {
            _rigidbody2D.position = position;
            _rigidbody2D.linearVelocity = Vector2.zero;
        }

        public void SetGravityScale(float gravityScale)
        {
            _rigidbody2D.gravityScale = gravityScale;
        }

        public void SetFacing(bool isFacingRight)
        {
            _spriteRenderer.flipX = !isFacingRight;
        }

        private void Reset()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
}
