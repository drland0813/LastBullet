using UnityEngine;

namespace LastBullet
{
    public class HealthBarsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private AimController _aimController;

        [Header("Bars")]
        [SerializeField] private DelayedHealthBar _playerBar;
        [SerializeField] private GameObject _targetBarRoot;
        [SerializeField] private DelayedHealthBar _targetBar;
        [SerializeField] [Min(0f)] private float _deadShowDuration = 0.6f;

        private Transform _currentTarget;
        private IHealth _currentTargetHealth;
        private float _hideTimer;

        private void Awake()
        {
            if (_playerHealth == null)
            {
                _playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            if (_aimController == null)
            {
                _aimController = FindFirstObjectByType<AimController>();
            }
        }

        private void Update()
        {
            UpdatePlayerBar();
            UpdateTargetBar();
        }

        private void UpdatePlayerBar()
        {
            if (_playerBar == null || _playerHealth == null) return;
            _playerBar.SetValue(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        }

        private void UpdateTargetBar()
        {
            Transform target = _aimController != null ? _aimController.CurrentTarget : null;

            if (target != _currentTarget)
            {
                if (_currentTargetHealth != null && _currentTargetHealth.CurrentHealth <= 0f)
                {
                    _targetBar?.SnapTo(0f, 1f);
                    _hideTimer = _deadShowDuration;
                }
                else
                {
                    _hideTimer = 0f;
                }

                _currentTarget = target;
                _currentTargetHealth = target != null ? target.GetComponentInParent<IHealth>() : null;

                if (_targetBar != null && _currentTargetHealth != null)
                {
                    _targetBar.SnapTo(_currentTargetHealth.CurrentHealth, _currentTargetHealth.MaxHealth);
                }
            }

            bool hasTarget = _currentTarget != null && _currentTargetHealth != null;

            if (!hasTarget && _hideTimer > 0f)
            {
                _hideTimer -= Time.deltaTime;
                hasTarget = true;
            }

            if (_targetBarRoot != null)
            {
                _targetBarRoot.SetActive(hasTarget);
            }

            if (hasTarget && _targetBar != null && _currentTargetHealth != null)
            {
                _targetBar.SetValue(_currentTargetHealth.CurrentHealth, _currentTargetHealth.MaxHealth);
            }
        }
    }
}
