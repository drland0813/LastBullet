using UnityEngine;

namespace LastBullet
{
    public class AimController : MonoBehaviour
    {
        [Header("Auto-Aim Settings")]
        [SerializeField] float _aimRadius = 10f;
        [SerializeField] LayerMask _enemyLayer;

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
                Vector3 toTarget = _currentTarget.position - transform.position;
                toTarget.y = 0f;
                return toTarget.normalized;
            }

            return transform.forward * 10f;
        }

        private Transform FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _aimRadius, _enemyLayer);

            if (hits.Length == 0) return null;

            Transform nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider hit in hits)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hit.transform;
                }
            }

            return nearest;
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