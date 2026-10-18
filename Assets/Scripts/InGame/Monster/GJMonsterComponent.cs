using UnityEngine;

namespace GameJam.InGame.Monster
{
    // View of a monster. Moved by a kinematic Rigidbody2D so the monster only moves through GJMonster updates
    // (this is what lets the Flow slowdown scale monster movement).
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class GJMonsterComponent : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Collider2D _collider2D;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        private Color _normalColor = Color.white;

        public Vector2 Position => _rigidbody2D.position;
        public Collider2D Collider => _collider2D;
        [SerializeField] public GameObject projectile;

        public void Init()
        {
            _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            // Contact is checked by polling (Collider2D.Distance), so the monster should never push the player physically.
            _collider2D.isTrigger = true;
            if (_spriteRenderer != null)
            {
                _normalColor = _spriteRenderer.color;
            }
        }

        public void MoveTowards(Vector2 targetPosition, float maxDistance)
        {
            _rigidbody2D.MovePosition(Vector2.MoveTowards(_rigidbody2D.position, targetPosition, maxDistance));
        }

        public void SetFacing(bool isFacingRight)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = !isFacingRight;
            }
        }

        public bool IsOverlapping(Collider2D other)
        {
            return _collider2D.Distance(other).isOverlapped;
        }

        // blinkRatio 1 = full blink color, 0 = normal color.
        public void SetDamageBlink(Color blinkColor, float blinkRatio)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.Lerp(_normalColor, blinkColor, blinkRatio);
            }
        }

        public void DestroyComponent()
        {
            Destroy(gameObject);
        }

        private void Reset()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _collider2D = GetComponent<Collider2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
}
