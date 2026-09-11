using UnityEngine;

namespace LastBullet
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public abstract class ProjectileBase : MonoBehaviour
    {
        protected float _speed = 10;
        protected float _damage = 10;
        protected float _lifetime = 1;
        protected Vector3 _direction;
        protected GameObject _hitImpactPrefab;
        protected ObjectPool<BulletProjectile> _pool;

        private Rigidbody _rigidbody;
        private float _timer;
        private bool _isActive;

        protected virtual void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.drag = 0f;
            _rigidbody.angularDrag = 0f;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _isActive = true;
        }

        public virtual void Launch(Vector3 origin, Vector3 direction, float speed,
                                   float damage, float lifetime, GameObject hitImpactPrefab,
                                   ObjectPool<BulletProjectile> pool)
        {
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);

            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _lifetime = lifetime;
            _hitImpactPrefab = hitImpactPrefab;
            _pool = pool;
            _timer = 0f;
            _isActive = true;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.velocity = _direction * _speed;
        }

        protected virtual void Update()
        {
            if (!_isActive) return;

            _timer += Time.deltaTime;
            if (_timer >= _lifetime)
            {
                ReturnToPool();
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!_isActive) return;

            OnHit(other);
            SpawnHitImpact(other);
            ReturnToPool();
        }

        protected abstract void OnHit(Collider other);

        protected void SpawnHitImpact(Collider other)
        {
            if (_hitImpactPrefab == null) return;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;

            if (hitNormal == Vector3.zero)
            {
                hitNormal = -_direction;
            }

            GameObject impact = Instantiate(_hitImpactPrefab, hitPoint,
                                                   Quaternion.LookRotation(hitNormal));
            Destroy(impact, 2f);
        }

        protected virtual void ReturnToPool()
        {
            if (!_isActive) return;

            _isActive = false;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _timer = 0f;

            if (_pool != null)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                _pool.Store(this as BulletProjectile);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}