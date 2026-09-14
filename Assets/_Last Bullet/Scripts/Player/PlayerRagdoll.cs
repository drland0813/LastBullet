using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LastBullet
{
    public class PlayerRagdoll : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _mainController;
        [SerializeField] private TopDownThirdPersonController _moveController;
        [SerializeField] private PlayerInputs _inputs;
        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private AimController _aimController;
        [SerializeField] private CharacterAnimationController _animationController;
        [SerializeField] private RigBuilder _rigBuilder;
        [SerializeField] private BasicRigidBodyPush _rigidBodyPush;

        private Rigidbody[] _ragdollBodies;
        private Collider[] _boneColliders;
        private bool _isRagdollActive;

        public bool IsRagdollActive => _isRagdollActive;
        public event Action OnRagdollEnabled;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_mainController == null)
            {
                _mainController = GetComponent<CharacterController>();
            }

            if (_moveController == null)
            {
                _moveController = GetComponent<TopDownThirdPersonController>();
            }

            if (_inputs == null)
            {
                _inputs = GetComponent<PlayerInputs>();
            }

            if (_aimController == null)
            {
                _aimController = GetComponent<AimController>();
            }

            if (_animationController == null)
            {
                _animationController = GetComponent<CharacterAnimationController>();
            }

            if (_rigBuilder == null)
            {
                _rigBuilder = GetComponent<RigBuilder>();
            }

            if (_rigidBodyPush == null)
            {
                _rigidBodyPush = GetComponent<BasicRigidBodyPush>();
            }

            _ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
            _boneColliders = GetComponentsInChildren<Collider>(true);

            SleepRagdoll();
        }

        public void EnableRagdoll(Vector3 point, Vector3 direction, float force)
        {
            if (_isRagdollActive) return;
            _isRagdollActive = true;

            SetGameplayComponents(false);

            if (_mainController != null)
            {
                _mainController.enabled = false;
            }

            EnableBoneColliders(true);
            SetKinematic(false);
            ApplyImpulse(point, direction, force);

            OnRagdollEnabled?.Invoke();
        }

        public void ResetRagdoll()
        {
            _isRagdollActive = false;

            SleepRagdoll();
            ResetBodiesVelocity();

            if (_animator != null)
            {
                _animator.enabled = true;
                _animator.Rebind();
            }

            if (_mainController != null)
            {
                _mainController.enabled = true;
            }

            SetGameplayComponents(true);
            ClearInputs();
        }

        private void ClearInputs()
        {
            if (_inputs == null) return;

            _inputs.MoveInput(Vector2.zero);
            _inputs.LookInput(Vector2.zero);
            _inputs.JumpInput(false);
            _inputs.SprintInput(false);
            _inputs.FireInput(false);
            _inputs.ReloadInput(false);
        }

        private void SleepRagdoll()
        {
            SetKinematic(true);
            EnableBoneColliders(false);
        }

        private void SetGameplayComponents(bool enabled)
        {
            if (_animator != null && !enabled)
            {
                _animator.enabled = false;
            }

            if (_moveController != null)
            {
                _moveController.enabled = enabled;
            }

            if (_inputs != null)
            {
                _inputs.enabled = enabled;
            }

            if (_weaponController != null)
            {
                _weaponController.enabled = enabled;
            }

            if (_aimController != null)
            {
                _aimController.enabled = enabled;
            }

            if (_animationController != null)
            {
                _animationController.enabled = enabled;
            }

            if (_rigBuilder != null)
            {
                _rigBuilder.enabled = enabled;
            }

            if (_rigidBodyPush != null)
            {
                _rigidBodyPush.enabled = enabled;
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

        private void EnableBoneColliders(bool enabled)
        {
            if (_boneColliders == null) return;

            foreach (Collider boneCollider in _boneColliders)
            {
                if (boneCollider == null) continue;
                if (boneCollider == _mainController) continue;
                if (boneCollider is CharacterController) continue;
                boneCollider.enabled = enabled;
            }
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

        private void ApplyImpulse(Vector3 point, Vector3 direction, float force)
        {
            if (force <= 0f) return;
            if (_ragdollBodies == null || _ragdollBodies.Length == 0) return;

            Vector3 pushDirection = direction;
            if (pushDirection == Vector3.zero)
            {
                pushDirection = transform.forward;
            }
            pushDirection.y = Mathf.Max(pushDirection.y, 0.1f);
            pushDirection = pushDirection.normalized;

            Rigidbody closestBody = FindClosestBody(point);
            if (closestBody != null)
            {
                closestBody.AddForceAtPosition(pushDirection * force, point, ForceMode.Impulse);
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
    }
}
