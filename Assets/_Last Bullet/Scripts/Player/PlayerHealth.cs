using System;
using UnityEngine;

namespace LastBullet
{
    public class PlayerHealth : MonoBehaviour, IDamageable, IHealth
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float _maxHealth = 100f;
        [SerializeField] private CharacterAnimationController _animationController;
        [SerializeField] private PlayerRagdoll _ragdoll;

        [Header("Hit Reaction")]
        [Tooltip("Time in seconds the player cannot move after taking a hit.")]
        [SerializeField] [Min(0f)] private float _hitStunDuration = 0.6f;

        private float _currentHealth;
        private bool _isDead;
        private float _hitStunEndTime = -1f;

        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _isDead;
        public bool IsStunned => Time.time < _hitStunEndTime;
        public bool IsMovementLocked => _isDead || IsStunned;
        public event Action OnDied;

        private void Awake()
        {
            if (_animationController == null)
            {
                _animationController = GetComponent<CharacterAnimationController>();
            }

            if (_ragdoll == null)
            {
                _ragdoll = GetComponent<PlayerRagdoll>();
            }

            _currentHealth = _maxHealth;
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
            if (_isDead || hit.Amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - hit.Amount);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                _ragdoll?.EnableRagdoll(hit.Point, hit.Direction, hit.Force);
                PlaySoundAt(SoundId.PlayerDie);
                OnDied?.Invoke();
                return;
            }

            _hitStunEndTime = Time.time + _hitStunDuration;
            _animationController?.PlayHitAnimation();
            PlaySoundAt(SoundId.PlayerHit);
        }

        public void RestoreHealth(float amount)
        {
            if (_isDead || amount <= 0f) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        }

        public void ResetHealth()
        {
            _isDead = false;
            _currentHealth = _maxHealth;
            _hitStunEndTime = -1f;
            _ragdoll?.ResetRagdoll();
        }

        private void PlaySoundAt(SoundId id)
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySfxAt(id, transform.position);
        }
    }
}
