using System;
using System.Collections;
using UnityEngine;

namespace LastBullet
{
    public class ExplosiveBarrel : MonoBehaviour, IDamageable, IHealth
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float _maxHealth = 30f;

        [Header("Explosion")]
        [SerializeField] [Min(0f)] private float _explosionRadius = 5f;
        [SerializeField] [Min(0f)] private float _explosionDamage = 500f;
        [SerializeField] [Min(0f)] private float _playerDamage = 40f;
        [SerializeField] [Min(0f)] private float _explosionForce = 50f;
        [SerializeField] [Min(0f)] private float _fuseDelay = 0.15f;
        [SerializeField] private GameObject _explosionVFX;
        [SerializeField] [Min(0f)] private float _explosionVFXLifetime = 3f;

        private float _currentHealth;
        private bool _exploded;
        private Coroutine _explodeCoroutine;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public bool HasExploded => _exploded;
        public event Action<ExplosiveBarrel> OnExploded;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        private void OnDestroy()
        {
            if (_explodeCoroutine != null)
            {
                StopCoroutine(_explodeCoroutine);
                _explodeCoroutine = null;
            }
        }

        public void TakeDamage(float amount, GameObject instigator)
        {
            TakeDamage(new HitData
            {
                Amount = amount,
                Instigator = instigator,
                Point = transform.position,
                Direction = Vector3.zero,
                Force = 0f
            });
        }

        public void TakeDamage(HitData hit)
        {
            if (_exploded || hit.Amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - hit.Amount);

            if (_currentHealth <= 0f)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            if (_explodeCoroutine != null)
            {
                StopCoroutine(_explodeCoroutine);
            }
            _explodeCoroutine = StartCoroutine(ExplodeSequence());
        }

        private IEnumerator ExplodeSequence()
        {
            if (_fuseDelay > 0f)
            {
                yield return new WaitForSeconds(_fuseDelay);
            }

            Vector3 center = transform.position + Vector3.up * 1.5f;
            SpawnExplosionVFX(center);
            PlayExplosionSound(center);
            DamageInRadius(center);

            _explodeCoroutine = null;
            OnExploded?.Invoke(this);
            Destroy(gameObject);
        }

        private void SpawnExplosionVFX(Vector3 center)
        {
            if (_explosionVFX == null) return;

            GameObject explosion = Instantiate(_explosionVFX, center, Quaternion.identity);
            ForcePlayParticles(explosion);

            if (_explosionVFXLifetime > 0f)
            {
                Destroy(explosion, _explosionVFXLifetime);
            }
        }

        private void ForcePlayParticles(GameObject explosion)
        {
            ParticleSystem[] systems = explosion.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem system in systems)
            {
                if (system == null || !system.gameObject.activeInHierarchy) continue;
                system.Play(true);
            }
        }

        private void PlayExplosionSound(Vector3 center)
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySfxAt(SoundId.Explosion, center);
        }

        private void DamageInRadius(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, _explosionRadius);

            foreach (Collider hitCollider in hits)
            {
                if (hitCollider == null) continue;

                IDamageable damageable = FindDamageable(hitCollider);
                if (damageable == null || ReferenceEquals(damageable, this)) continue;

                Vector3 victimPosition = hitCollider.transform.position;
                Vector3 blastDirection = victimPosition - center;
                blastDirection.y = Mathf.Max(blastDirection.y, 0.5f);
                if (blastDirection == Vector3.zero)
                {
                    blastDirection = Vector3.up;
                }

                bool isPlayer = hitCollider.GetComponentInParent<PlayerHealth>() != null;

                damageable.TakeDamage(new HitData
                {
                    Amount = isPlayer ? _playerDamage : _explosionDamage,
                    Instigator = gameObject,
                    Point = victimPosition,
                    Direction = blastDirection.normalized,
                    Force = _explosionForce
                });
            }
        }

        private IDamageable FindDamageable(Collider hitCollider)
        {
            MonoBehaviour[] components = hitCollider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1f, _explosionRadius);
        }
    }
}
