using UnityEngine;

namespace LastBullet
{
    [CreateAssetMenu(fileName = "NewZombieStats", menuName = "Last Bullet/Enemy/Zombie Stats")]
    public class ZombieStatsSO : ScriptableObject
    {
        [Header("Health")]
        [SerializeField] [Min(1f)] private float _maxHealth = 100f;

        [Header("Movement")]
        [SerializeField] [Min(0f)] private float _moveSpeed = 2f;
        [SerializeField] [Min(0f)] private float _wanderSpeed = 1f;
        [SerializeField] [Min(0f)] private float _detectionRange = 10f;
        [SerializeField] [Min(0f)] private float _loseTargetRange = 15f;
        [SerializeField] [Min(0f)] private float _wanderRadius = 8f;
        [SerializeField] [Min(0.1f)] private float _wanderInterval = 3f;
        [SerializeField] [Min(0f)] private float _turnSpeed = 8f;

        [Header("Attack")]
        [SerializeField] [Min(0f)] private float _attackRange = 1.5f;
        [SerializeField] [Min(0f)] private float _attackDamage = 10f;
        [SerializeField] [Min(0.01f)] private float _attackCooldown = 1.2f;
        [SerializeField] [Min(0f)] private float _attackDelay = 0.25f;
        [SerializeField] [Min(0.01f)] private float _attackAnimationDuration = 0.8f;

        [Header("Reaction")]
        [SerializeField] [Min(0f)] private float _hitStunDuration = 0.4f;
        [SerializeField] private string _targetTag = "Player";

        [Header("Ragdoll")]
        [SerializeField] [Min(0f)] private float _corpseLifetime = 6f;
        [SerializeField] [Min(0f)] private float _ragdollForceMultiplier = 1f;
        [SerializeField] [Min(0f)] private float _dissolveDuration = 1f;

        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float WanderSpeed => _wanderSpeed;
        public float DetectionRange => _detectionRange;
        public float LoseTargetRange => Mathf.Max(_loseTargetRange, _detectionRange);
        public float WanderRadius => _wanderRadius;
        public float WanderInterval => _wanderInterval;
        public float TurnSpeed => _turnSpeed;
        public float AttackRange => _attackRange;
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float AttackDelay => _attackDelay;
        public float AttackAnimationDuration => _attackAnimationDuration;
        public float HitStunDuration => _hitStunDuration;
        public string TargetTag => _targetTag;
        public float CorpseLifetime => _corpseLifetime;
        public float RagdollForceMultiplier => _ragdollForceMultiplier;
        public float DissolveDuration => _dissolveDuration;
    }
}
