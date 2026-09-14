using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour, IDamageable, IHealth
    {
        private enum ZombieState
        {
            Idle,
            Wander,
            Chase,
            Attack,
            Hit,
            Dead
        }

        [Header("References")]
        [SerializeField] private ZombieStatsSO _stats;
        [SerializeField] private Animator _animator;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Transform _target;
        [SerializeField] private Collider[] _colliders;
        [SerializeField] private ZombieRagdoll _ragdoll;
        [SerializeField] private ZombieDissolve _dissolve;
        [SerializeField] private PickupDropper _dropper;

        [Header("Effects")]
        [SerializeField] private GameObject _bloodImpactPrefab;

        [Header("Behaviour")]
        [SerializeField] private bool _findTargetOnStart = true;
        [SerializeField] private bool _disableCollidersOnDeath = true;
        [SerializeField] private bool _useRagdoll = true;

        private static readonly int ParamIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int ParamIsChasing = Animator.StringToHash("IsChasing");
        private static readonly int ParamAttack = Animator.StringToHash("Attack");
        private static readonly int ParamHit = Animator.StringToHash("Hit");
        private static readonly int ParamDie = Animator.StringToHash("Die");

        private ZombieState _state;
        private ZombieState _stateBeforeHit;
        private float _currentHealth;
        private float _nextWanderTime;
        private float _nextAttackTime;
        private float _attackStartedTime;
        private float _attackAnimationEndTime;
        private float _hitEndTime;
        private float _nextTargetSearchTime;
        private bool _attackDamageApplied;
        private Vector3 _homePosition;
        private bool _targetWarningShown;
        private bool _bloodWarningShown;
        private HitData _lastHit;
        private Coroutine _fallbackDespawnCoroutine;
        private float _speedMultiplier = 1f;
        private float _healthMultiplier = 1f;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _stats != null ? _stats.MaxHealth : 100f;
        public bool IsDead => _state == ZombieState.Dead;
        public GameObject BloodImpactPrefab => _bloodImpactPrefab;
        public event Action<ZombieAI> OnDespawnReady;
        public event Action<ZombieAI> OnKilled;

        public bool TryGetBloodImpact(out GameObject bloodPrefab)
        {
            bloodPrefab = _bloodImpactPrefab;

            if (bloodPrefab == null && !_bloodWarningShown)
            {
                _bloodWarningShown = true;
                Debug.LogWarning($"[ZombieAI] BloodImpactPrefab is missing on '{name}'. Drag a blood VFX prefab into Blood Impact Prefab to show blood on hit.", this);
            }

            return bloodPrefab != null;
        }

        private void Reset()
        {
            _agent = GetComponent<NavMeshAgent>();
            _animator = GetComponentInChildren<Animator>();
            _colliders = GetComponentsInChildren<Collider>();
            _ragdoll = GetComponent<ZombieRagdoll>();
            _dissolve = GetComponent<ZombieDissolve>();
            _dropper = GetComponent<PickupDropper>();
        }

        private void Awake()
        {
            if (_agent == null)
            {
                _agent = GetComponent<NavMeshAgent>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_colliders == null || _colliders.Length == 0)
            {
                _colliders = GetComponentsInChildren<Collider>();
            }

            if (_ragdoll == null)
            {
                _ragdoll = GetComponent<ZombieRagdoll>();
            }

            if (_dissolve == null)
            {
                _dissolve = GetComponent<ZombieDissolve>();
            }

            if (_dropper == null)
            {
                _dropper = GetComponent<PickupDropper>();
            }

            if (_stats == null)
            {
                Debug.LogError("[ZombieAI] ZombieStatsSO is not assigned.", this);
                enabled = false;
                return;
            }

            if (_animator != null)
            {
                _animator.applyRootMotion = false;
            }

            _currentHealth = _stats.MaxHealth;
            _homePosition = transform.position;
            _state = ZombieState.Idle;
            _nextWanderTime = Time.time + _stats.WanderInterval;
        }

        private void Start()
        {
            if (_findTargetOnStart && _target == null)
            {
                FindTarget();
            }
        }

        public void SetDifficultyMultipliers(float speedMultiplier, float healthMultiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            _healthMultiplier = Mathf.Max(0.1f, healthMultiplier);
        }

        public void SpawnAt(Vector3 position)
        {
            StopFallbackDespawn();
            _ragdoll?.ResetRagdoll();
            _dissolve?.RestoreOriginalMaterials();
            ResetState(position);
            gameObject.SetActive(true);
        }

        private void ResetState(Vector3 position)
        {
            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.Rebind();
                _animator.applyRootMotion = false;
            }

            if (_agent != null)
            {
                _agent.enabled = true;
                _agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }

            transform.rotation = Quaternion.identity;
            EnableMainColliders();

            _currentHealth = (_stats != null ? _stats.MaxHealth : 100f) * _healthMultiplier;
            _homePosition = position;
            _state = ZombieState.Idle;
            _stateBeforeHit = ZombieState.Idle;
            _nextWanderTime = Time.time + (_stats != null ? _stats.WanderInterval : 3f);
            _nextAttackTime = 0f;
            _attackStartedTime = 0f;
            _attackAnimationEndTime = 0f;
            _hitEndTime = 0f;
            _nextTargetSearchTime = 0f;
            _attackDamageApplied = false;
            _target = null;
            _targetWarningShown = false;
            _lastHit = default;

            if (_findTargetOnStart)
            {
                FindTarget();
            }
        }

        private void EnableMainColliders()
        {
            if (_colliders == null) return;

            foreach (Collider currentCollider in _colliders)
            {
                if (currentCollider == null) continue;
                if (IsRagdollCollider(currentCollider)) continue;
                currentCollider.enabled = true;
            }
        }

        private void OnRagdollCorpseFinished()
        {
            if (_ragdoll != null)
            {
                _ragdoll.OnCorpseFinished -= OnRagdollCorpseFinished;
            }
            OnDespawnReady?.Invoke(this);
        }

        private void StartFallbackDespawn(float delay)
        {
            StopFallbackDespawn();
            _fallbackDespawnCoroutine = StartCoroutine(FallbackDespawnAfterDelay(delay));
        }

        private void StopFallbackDespawn()
        {
            if (_fallbackDespawnCoroutine != null)
            {
                StopCoroutine(_fallbackDespawnCoroutine);
                _fallbackDespawnCoroutine = null;
            }
        }

        private IEnumerator FallbackDespawnAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            _fallbackDespawnCoroutine = null;
            OnDespawnReady?.Invoke(this);
        }

        private void Update()
        {
            if (_state == ZombieState.Dead) return;

            DropDeadTarget();

            if (UpdateHitState()) return;

            if (_target == null && Time.time >= _nextTargetSearchTime)
            {
                FindTarget();
            }

            switch (_state)
            {
                case ZombieState.Idle:
                    UpdateIdle();
                    break;
                case ZombieState.Wander:
                    UpdateWander();
                    break;
                case ZombieState.Chase:
                    UpdateChase();
                    break;
                case ZombieState.Attack:
                    UpdateAttack();
                    break;
            }

            UpdateAnimatorMovement();
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
            if (IsDead || hit.Amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - hit.Amount);
            _lastHit = hit;

            if (_currentHealth <= 0f)
            {
                Die(hit);
                return;
            }

            _stateBeforeHit = _state == ZombieState.Attack
                ? ZombieState.Chase
                : _state;
            _state = ZombieState.Hit;
            _hitEndTime = Time.time + _stats.HitStunDuration;
            StopMovement();
            UpdateAnimatorMovement();
            SetTrigger(ParamHit);
            PlaySoundAt(SoundId.ZombieHit, transform.position);
        }

        public void SetTarget(Transform target)
        {
            if (IsDead) return;
            _target = target;
        }

        private bool UpdateHitState()
        {
            if (_state != ZombieState.Hit) return false;
            if (Time.time < _hitEndTime)
            {
                StopMovement();
                return true;
            }

            ResolveStateAfterHit();
            return false;
        }

        private void ResolveStateAfterHit()
        {
            if (_target == null)
            {
                FindTarget();
            }

            if (_target == null)
            {
                _state = ZombieState.Idle;
                return;
            }

            float distance = GetTargetDistance();
            if (distance <= _stats.AttackRange)
            {
                _state = ZombieState.Attack;
                _attackStartedTime = 0f;
                _attackAnimationEndTime = 0f;
                return;
            }

            if (distance <= _stats.LoseTargetRange)
            {
                _state = ZombieState.Chase;
                return;
            }

            _state = _stateBeforeHit == ZombieState.Wander
                ? ZombieState.Wander
                : ZombieState.Idle;
        }

        private void UpdateIdle()
        {
            StopMovement();

            if (HasTargetInRange(_stats.DetectionRange))
            {
                _state = ZombieState.Chase;
                return;
            }

            if (Time.time >= _nextWanderTime && _stats.WanderRadius > 0f)
            {
                SetWanderDestination();
            }
        }

        private void UpdateWander()
        {
            if (HasTargetInRange(_stats.DetectionRange))
            {
                _state = ZombieState.Chase;
                return;
            }

            if (!_agent.hasPath || _agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
            {
                _state = ZombieState.Idle;
                _nextWanderTime = Time.time + _stats.WanderInterval;
                StopMovement();
            }
        }

        private void UpdateChase()
        {
            if (_target == null)
            {
                _state = ZombieState.Idle;
                return;
            }

            float distance = GetTargetDistance();
            if (distance > _stats.LoseTargetRange)
            {
                _target = null;
                _state = ZombieState.Idle;
                StopMovement();
                return;
            }

            if (distance <= _stats.AttackRange)
            {
                _state = ZombieState.Attack;
                _attackStartedTime = 0f;
                _attackAnimationEndTime = 0f;
                StopMovement();
                return;
            }

            MoveTo(_target.position, _stats.MoveSpeed);
        }

        private void UpdateAttack()
        {
            if (_target == null)
            {
                _state = ZombieState.Idle;
                return;
            }

            StopMovement();
            FaceTarget();

            if (_attackStartedTime > 0f)
            {
                if (Time.time >= _attackStartedTime + _stats.AttackDelay)
                {
                    ApplyAttackDamage();
                }

                if (Time.time >= _attackAnimationEndTime)
                {
                    _attackStartedTime = 0f;
                }

                return;
            }

            float distance = GetTargetDistance();
            if (distance > _stats.AttackRange)
            {
                _state = ZombieState.Chase;
                return;
            }

            if (Time.time < _nextAttackTime) return;

            _attackStartedTime = Time.time;
            _attackAnimationEndTime = Time.time + _stats.AttackAnimationDuration;
            _attackDamageApplied = false;
            SetTrigger(ParamAttack);
            PlaySoundAt(SoundId.ZombieAttack, transform.position);

            if (_stats.AttackDelay <= 0f)
            {
                ApplyAttackDamage();
            }
        }

        private void ApplyAttackDamage()
        {
            if (_attackDamageApplied || _target == null) return;

            _attackDamageApplied = true;
            _nextAttackTime = Time.time + _stats.AttackCooldown;

            IDamageable damageable = FindDamageable(_target);
            damageable?.TakeDamage(_stats.AttackDamage, gameObject);
        }

        private void SetWanderDestination()
        {
            Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * _stats.WanderRadius;
            Vector3 candidate = _homePosition + new Vector3(randomPoint.x, 0f, randomPoint.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _stats.WanderRadius, NavMesh.AllAreas))
            {
                _state = ZombieState.Wander;
                MoveTo(hit.position, _stats.WanderSpeed);
            }
            else
            {
                _nextWanderTime = Time.time + _stats.WanderInterval;
            }
        }

        private void FindTarget()
        {
            _nextTargetSearchTime = Time.time + 1f;
            if (!string.IsNullOrWhiteSpace(_stats.TargetTag))
            {
                GameObject targetObject = GameObject.FindGameObjectWithTag(_stats.TargetTag);
                if (targetObject != null && IsTargetValid(targetObject.transform))
                {
                    _target = targetObject.transform;
                    return;
                }
            }

            TopDownThirdPersonController player = FindFirstObjectByType<TopDownThirdPersonController>();
            if (player != null && IsTargetValid(player.transform))
            {
                _target = player.transform;
                return;
            }

            if (!_targetWarningShown)
            {
                _targetWarningShown = true;
                Debug.LogWarning(
                    $"[ZombieAI] No target found. Assign a player with tag '{_stats.TargetTag}' or assign _target manually.",
                    this);
            }
        }

        private bool HasTargetInRange(float range)
        {
            return _target != null && GetTargetDistance() <= range;
        }

        private void DropDeadTarget()
        {
            if (_target == null || IsTargetValid(_target)) return;

            _target = null;
            _attackStartedTime = 0f;
            _attackDamageApplied = false;

            if (_state == ZombieState.Chase || _state == ZombieState.Attack)
            {
                _state = ZombieState.Idle;
                StopMovement();
            }
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;

            PlayerHealth health = target.GetComponentInParent<PlayerHealth>();
            return health == null || !health.IsDead;
        }

        private float GetTargetDistance()
        {
            Vector3 offset = _target.position - transform.position;
            offset.y = 0f;
            return offset.magnitude;
        }

        private void MoveTo(Vector3 destination, float speed)
        {
            if (!_agent.isOnNavMesh) return;

            _agent.isStopped = false;
            _agent.speed = speed * _speedMultiplier;
            _agent.stoppingDistance = Mathf.Max(0f, _stats.AttackRange * 0.8f);
            _agent.SetDestination(destination);
        }

        private void StopMovement()
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }

        private void FaceTarget()
        {
            if (_target == null) return;

            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _stats.TurnSpeed * Time.deltaTime);
        }

        private void UpdateAnimatorMovement()
        {
            if (_animator == null) return;

            bool isMoving = _state == ZombieState.Wander || _state == ZombieState.Chase;
            bool isLocked = _state == ZombieState.Attack
                || _state == ZombieState.Hit
                || _state == ZombieState.Dead;

            if (isLocked)
            {
                isMoving = false;
            }

            _animator.SetBool(ParamIsMoving, isMoving && _agent.velocity.sqrMagnitude > 0.01f);
            _animator.SetBool(ParamIsChasing, !isLocked && _state == ZombieState.Chase);
        }

        private void Die(HitData hit)
        {
            _state = ZombieState.Dead;
            OnKilled?.Invoke(this);
            StopMovement();
            DisableAgent();
            _dropper?.DropAt(transform.position);
            PlaySoundAt(SoundId.ZombieDie, transform.position);

            if (_useRagdoll && _ragdoll != null)
            {
                DisableMainColliders();
                float corpseLifetime = _stats != null ? _stats.CorpseLifetime : 6f;
                float forceMultiplier = _stats != null ? _stats.RagdollForceMultiplier : 1f;
                float dissolveDuration = _stats != null ? _stats.DissolveDuration : 1f;
                _ragdoll.OnCorpseFinished -= OnRagdollCorpseFinished;
                _ragdoll.OnCorpseFinished += OnRagdollCorpseFinished;
                _ragdoll.EnableRagdoll(hit.Point, hit.Direction, hit.Force * forceMultiplier, corpseLifetime, dissolveDuration);
                return;
            }

            SetTrigger(ParamDie);

            if (_disableCollidersOnDeath)
            {
                DisableMainColliders();
            }

            float fallbackDelay = _stats != null ? _stats.CorpseLifetime + _stats.DissolveDuration : 7f;
            StartFallbackDespawn(fallbackDelay);
        }

        private void DisableAgent()
        {
            if (_agent == null) return;

            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            _agent.enabled = false;
        }

        private void DisableMainColliders()
        {
            if (_colliders == null) return;

            foreach (Collider currentCollider in _colliders)
            {
                if (currentCollider == null) continue;
                if (IsRagdollCollider(currentCollider)) continue;

                currentCollider.enabled = false;
            }
        }

        private bool IsRagdollCollider(Collider currentCollider)
        {
            if (currentCollider == null) return false;
            if (currentCollider.transform != transform && currentCollider.GetComponent<Rigidbody>() != null) return true;
            return false;
        }

        private void SetTrigger(int parameter)
        {
            if (_animator != null)
            {
                _animator.SetTrigger(parameter);
            }
        }

        private void PlaySoundAt(SoundId id, Vector3 position)
        {
            if (SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySfxAt(id, position);
        }

        private IDamageable FindDamageable(Transform target)
        {
            MonoBehaviour[] components = target.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }
    }
}
