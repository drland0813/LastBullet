using System;
using System.Collections;
using UnityEngine;

namespace LastBullet
{
    public class ZombieRagdoll : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Collider _mainCollider;
        [SerializeField] private ZombieDissolve _dissolve;

        [Header("Death Impulse")]
        [Tooltip("Minimum impulse applied on death so the body always topples visibly.")]
        [SerializeField] [Min(0f)] private float _minDeathImpulse = 15f;

        private Rigidbody[] _ragdollBodies;
        private bool _isRagdollActive;
        private Coroutine _despawnCoroutine;

        public bool IsRagdollActive => _isRagdollActive;
        public bool IsDissolving => _dissolve != null && _dissolve.IsDissolving;
        public event Action OnCorpseFinished;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_mainCollider == null)
            {
                _mainCollider = GetComponent<Collider>();
            }

            if (_dissolve == null)
            {
                _dissolve = GetComponent<ZombieDissolve>();
            }

            _ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
            
            SetKinematic(true);
            EnableChildColliders(false);
        }

        private void OnDestroy()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }
        }

        private void EnableChildColliders(bool enable)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                if (col != _mainCollider)
                {
                    col.enabled = enable;
                }
            }
        }

        public void EnableRagdoll(Vector3 point, Vector3 direction, float force, float corpseLifetime, float dissolveDuration)
        {
            if (_isRagdollActive) return;
            ResetRagdoll();
            _isRagdollActive = true;

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            if (_mainCollider != null)
            {
                _mainCollider.enabled = false;
            }
            EnableChildColliders(true);

            if (_ragdollBodies == null || _ragdollBodies.Length == 0)
            {
                _ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
            }

            float finalForce = Mathf.Max(force, _minDeathImpulse);
            if (force <= 0f)
            {
                Debug.LogWarning($"[ZombieRagdoll] Zero death impulse on '{name}'. Using minimum {_minDeathImpulse}. Check weapon HitForce.", this);
            }

            SetKinematic(false);
            VerifyRagdoll(point);
            ApplyImpulse(point, direction, finalForce);

            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
            }
            _despawnCoroutine = StartCoroutine(DespawnAfterDelay(corpseLifetime, dissolveDuration));
        }

        public void ResetRagdoll()
        {
            if (_despawnCoroutine != null)
            {
                StopCoroutine(_despawnCoroutine);
                _despawnCoroutine = null;
            }

            _isRagdollActive = false;

            if (_animator != null)
            {
                _animator.enabled = true;
            }

            if (_mainCollider != null)
            {
                _mainCollider.enabled = true;
            }

            EnableChildColliders(false);
            SetKinematic(true);
            ResetBodiesVelocity();
            _dissolve?.RestoreOriginalMaterials();
        }

        private void ResetBodiesVelocity()
        {
            if (_ragdollBodies == null) return;

            foreach (Rigidbody body in _ragdollBodies)
            {
                if (body == null) continue;

                bool wasKinematic = body.isKinematic;
                body.isKinematic = false;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.Sleep();
                body.isKinematic = wasKinematic;
            }
        }

        private void SetKinematic(bool isKinematic)
        {
            if (_ragdollBodies == null) return;

            foreach (Rigidbody body in _ragdollBodies)
            {
                if (body == null) continue;
                body.isKinematic = isKinematic;
            }
        }

        private void VerifyRagdoll(Vector3 point)
        {
            if (_ragdollBodies == null || _ragdollBodies.Length == 0)
            {
                Debug.LogError($"[ZombieRagdoll] No Rigidbody found on '{name}'. Ragdoll cannot fall.", this);
                return;
            }

            int stuckCount = 0;
            foreach (Rigidbody body in _ragdollBodies)
            {
                if (body != null && body.isKinematic)
                {
                    stuckCount++;
                }
            }

            if (stuckCount == _ragdollBodies.Length)
            {
                Debug.LogError($"[ZombieRagdoll] All {_ragdollBodies.Length} bodies still kinematic on '{name}' (animatorNull={_animator == null}). Ragdoll cannot fall.", this);
            }
        }

        private void ApplyImpulse(Vector3 point, Vector3 direction, float force)
        {
            if (force <= 0f) return;
            if (_ragdollBodies == null || _ragdollBodies.Length == 0) return;

            Vector3 flatDirection = direction;
            flatDirection.y = Mathf.Max(flatDirection.y, 0.35f);
            if (flatDirection == Vector3.zero)
            {
                flatDirection = transform.forward;
            }
            flatDirection = flatDirection.normalized;

            Rigidbody closestBody = FindClosestBody(point);
            if (closestBody != null)
            {
                closestBody.AddForceAtPosition(flatDirection * force, point, ForceMode.Impulse);
            }
        }

        private Rigidbody FindClosestBody(Vector3 point)
        {
            Rigidbody closestBody = null;
            float closestSqrDistance = float.MaxValue;

            foreach (Rigidbody body in _ragdollBodies)
            {
                if (body == null) continue;

                float sqrDistance = (body.worldCenterOfMass - point).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestBody = body;
                }
            }

            return closestBody;
        }

        private IEnumerator DespawnAfterDelay(float corpseLifetime, float dissolveDuration)
        {
            if (corpseLifetime > 0f)
            {
                yield return new WaitForSeconds(corpseLifetime);
            }

            FreezeCorpse();

            if (_dissolve != null && dissolveDuration > 0f)
            {
                yield return _dissolve.DissolveOverTime(dissolveDuration);
            }

            _despawnCoroutine = null;
            OnCorpseFinished?.Invoke();
        }

        private void FreezeCorpse()
        {
            SetKinematic(true);
            EnableChildColliders(false);
        }
    }
}
