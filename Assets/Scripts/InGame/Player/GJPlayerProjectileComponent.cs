using UnityEngine;

namespace GameJam.InGame.Player
{
    // View of a player projectile. The prefab's sprite must point toward +X (right);
    // it is rotated so +X faces the fire direction.
    public class GJPlayerProjectileComponent : MonoBehaviour
    {
        // Hit area. Monster hits are polled by GJPlayerProjectileManager, so this is only used as a trigger shape.
        [SerializeField] private Collider2D _hitCollider;
        // Sprite child that spins. Kept separate from the root so the hit collider does not rotate with it.
        [SerializeField] private Transform _visualTransform;

        public Vector2 Position => transform.position;
        public Collider2D HitCollider => _hitCollider;

        public void Init(Vector2 position, Vector2 direction, float scale)
        {
            if (_hitCollider == null)
            {
                Debug.LogError($"Projectile '{name}' has no hit collider assigned. It will not damage monsters.");
            }
            else
            {
                _hitCollider.isTrigger = true;
            }
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            var angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
            transform.localScale = Vector3.one * scale;
        }

        public void Move(Vector2 deltaPosition)
        {
            transform.position += new Vector3(deltaPosition.x, deltaPosition.y, 0f);
        }

        // Positive degrees spin counterclockwise on screen.
        // Rotated around the world Z axis because the visual child may be flipped (e.g. 180 degrees on Y),
        // which would reverse a local Z rotation.
        public void SpinVisual(float deltaDegrees)
        {
            if (_visualTransform == null)
            {
                return;
            }
            _visualTransform.Rotate(0f, 0f, deltaDegrees, Space.World);
        }

        public void DestroyComponent()
        {
            Destroy(gameObject);
        }

        private void Reset()
        {
            _hitCollider = GetComponentInChildren<Collider2D>();
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _visualTransform = spriteRenderer != null ? spriteRenderer.transform : null;
        }
    }
}
