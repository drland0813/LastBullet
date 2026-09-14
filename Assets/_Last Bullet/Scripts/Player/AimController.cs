using UnityEngine;

namespace LastBullet
{
    public class AimController : MonoBehaviour
    {
        [Header("Auto-Aim Settings")]
        [SerializeField] private float _aimRadius = 10f;
        [SerializeField] private LayerMask _enemyLayer;
        [SerializeField] private Transform _rotationTarget;
        [SerializeField] [Min(0f)] private float _rotationSpeed = 15f;

        private Transform _currentTarget;

        public Transform CurrentTarget => _currentTarget;

        private void Update()
        {
            _currentTarget = FindNearestEnemy();
        }

        public Vector3 GetAimDirection()
        {
            if (_currentTarget != null)
            {
                Transform aimOrigin = _rotationTarget != null ? _rotationTarget : transform;
                Vector3 toTarget = _currentTarget.position - aimOrigin.position;
                toTarget.y = 0f;
                return toTarget.normalized;
            }

            Transform aimTransform = _rotationTarget != null ? _rotationTarget : transform;
            return aimTransform.forward;
        }

        public void RotateTowardsCurrentTarget()
        {
            if (_currentTarget == null) return;

            Transform rotationTarget = _rotationTarget != null ? _rotationTarget : transform;
            Vector3 direction = _currentTarget.position - rotationTarget.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            rotationTarget.rotation = Quaternion.Slerp(
                rotationTarget.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }

            public void SnapTowardsCurrentTarget()
            {
                if (_currentTarget == null) return;

                Transform rotationTarget = _rotationTarget != null ? _rotationTarget : transform;
                Vector3 direction = _currentTarget.position - rotationTarget.position;
                direction.y = 0f;

                if (direction.sqrMagnitude <= 0.001f) return;

                rotationTarget.rotation = Quaternion.LookRotation(direction.normalized);
            }

        private Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _aimRadius, _enemyLayer);

            if (hits.Length == 0) return null;

            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                if (hit == null) continue;
                if (IsDeadTarget(hit)) continue;

                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hit.transform;
                }
            }

            return nearest;
        }

        private bool IsDeadTarget(Collider hitCollider)
        {
            ZombieAI zombie = hitCollider.GetComponentInParent<ZombieAI>();
            return zombie != null && zombie.IsDead;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _currentTarget != null
                ? new Color(1f, 0f, 0f, 0.25f)
                : new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, _aimRadius);

            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }
    }
}