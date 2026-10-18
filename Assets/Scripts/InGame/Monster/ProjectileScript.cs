using UnityEngine;
using System;
using GameJam.InGame.Player;
using GameJam.InGame.System;
using Object = UnityEngine.Object;

    public class ProjectileScript : MonoBehaviour
    {
        // Hit area. Monster hits are polled by GJPlayerProjectileManager, so this is only used as a trigger shape.
        [SerializeField] private Collider2D _hitCollider;
        // Sprite child that spins. Kept separate from the root so the hit collider does not rotate with it.
        [SerializeField] private Transform _visualTransform;

        [SerializeField] private Rigidbody2D _rigidbody2D;

        public Vector2 Position => transform.position;
        public Collider2D HitCollider => _hitCollider;
        Vector2 targetPos;
        float maxD;

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
            transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees); //remove if the projectile doesn't need it
            transform.localScale = Vector3.one * scale;
        }

        public void setStuff(Vector2 targetPosition, float maxDistance)
        {
            _rigidbody2D.linearVelocity = new Vector2(targetPosition.x - this.Position.x, targetPosition.y - this.Position.y).normalized * 10f;
            maxD = maxDistance;
        }

        public void Move(Vector2 deltaPosition)
        {
            transform.position += new Vector3(deltaPosition.x, deltaPosition.y, 0f);
        }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

        //easy way to check for collision
        public void OnCollisionEnter2D(Collision2D collision)
        {
            //Debug.Log(collision.gameObject.name);
            if (collision.gameObject.CompareTag("Ground")) {
                Destroy(gameObject);
            }
            if (collision.gameObject.CompareTag("Player1")) {
                GJPlayerComponent player = collision.gameObject.GetComponent<GJPlayerComponent>();
                player.isHit = true;
                Destroy(gameObject);
            }
        }


        // public void MoveTowards()
        // {
        //     //_rigidbody2D.linearVelocity = targetPos;
        //     //Vector2.MoveTowards(_rigidbody2D.position, targetPosition, maxDistance);
        // }
        // Update is called once per frame
        void Update()
        {
            //oveTowards();
        }
    }
