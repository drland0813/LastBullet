using UnityEngine;

namespace LastBullet
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public abstract class ProjectileBase : MonoBehaviour
    {
        protected float _speed = 10;
        protected float _damage = 10;
        protected float _hitForce = 10f;
        protected float _lifetime = 1;
        protected Vector3 _direction;
        protected GameObject _hitImpactPrefab;
        protected ObjectPool<BulletProjectile> _pool;
        protected GameObject _instigator;

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
                                   float damage, float hitForce, float lifetime, GameObject hitImpactPrefab,
                                   ObjectPool<BulletProjectile> pool, GameObject instigator)
        {
            transform.position = origin;
            transform.rotation = Quaternion.LookRotation(direction);

            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _hitForce = hitForce;
            _lifetime = lifetime;
            _hitImpactPrefab = hitImpactPrefab;
            _pool = pool;
            _instigator = instigator;
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
            if (other.GetComponentInParent<PickupItem>() != null) return;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            HitData hit = new HitData
            {
                Amount = _damage,
                Instigator = _instigator,
                Point = hitPoint,
                Direction = _direction,
                Force = _hitForce
            };

            OnHit(other, hit);
            SpawnHitImpact(other);
            ReturnToPool();
        }

        protected abstract void OnHit(Collider other, HitData hit);

        protected void SpawnHitImpact(Collider other)
        {
            GameObject impactPrefab = ResolveImpactPrefab(other);
            if (impactPrefab == null) return;

            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPoint).normalized;

            if (hitNormal == Vector3.zero)
            {
                hitNormal = -_direction;
            }

            GameObject impact = Instantiate(impactPrefab, hitPoint,
                                                   Quaternion.LookRotation(hitNormal));
            Destroy(impact, 2f);
        }

        private GameObject ResolveImpactPrefab(Collider other)
        {
            ZombieAI zombie = other.GetComponentInParent<ZombieAI>();
            if (zombie != null && zombie.TryGetBloodImpact(out GameObject bloodPrefab))
            {
                return bloodPrefab;
            }

            return _hitImpactPrefab;
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