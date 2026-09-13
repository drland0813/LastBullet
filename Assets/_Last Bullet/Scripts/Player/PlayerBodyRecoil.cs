using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LastBullet
{
    [Serializable]
    public struct BodyRecoilSettings
    {
        [SerializeField] [Min(0f)] private float _impulse;
        [SerializeField] [Min(0f)] private float _recoilAngle;
        [SerializeField] [Min(0.01f)] private float _returnSpeed;
        [SerializeField] [Min(0f)] private float _maxAmount;

        public float Impulse => _impulse;
        public float RecoilAngle => _recoilAngle;
        public float ReturnSpeed => _returnSpeed;
        public float MaxAmount => _maxAmount;

        public static BodyRecoilSettings Default => new BodyRecoilSettings
        {
            _impulse = 1f,
            _recoilAngle = 5f,
            _returnSpeed = 15f,
            _maxAmount = 1.5f
        };
    }

    public class PlayerBodyRecoil : MonoBehaviour
    {
        [Header("Multi-Aim Recoil")]
        [SerializeField] private MultiAimConstraint _recoilConstraint;
        [SerializeField] private Transform _recoilTarget;

        private BodyRecoilSettings _settings = BodyRecoilSettings.Default;
        private float _amount;
        private Quaternion _initialLocalRotation;

        private void Awake()
        {
            if (_recoilConstraint == null || _recoilTarget == null)
            {
                Debug.LogWarning("[PlayerBodyRecoil] Recoil constraint and target must be assigned.", this);
            }

            if (_recoilTarget != null)
            {
                _initialLocalRotation = _recoilTarget.localRotation;
            }

            SetConstraintWeight(0f);
        }

        public void Play(BodyRecoilSettings settings)
        {
            if (settings.MaxAmount <= 0f || settings.ReturnSpeed <= 0f)
            {
                settings = BodyRecoilSettings.Default;
            }

            _settings = settings;
            _amount = Mathf.Min(
                _amount + Mathf.Max(0f, settings.Impulse),
                Mathf.Max(0f, settings.MaxAmount));
            SetConstraintWeight(1f);
        }

        public void ResetMotion()
        {
            _amount = 0f;
            ResetTargetRotation();
            SetConstraintWeight(0f);
        }

        private void LateUpdate()
        {
            if (_amount <= 0.001f)
            {
                ResetMotion();
                return;
            }

            _amount = Mathf.MoveTowards(_amount, 0f, _settings.ReturnSpeed * Time.deltaTime);
            ApplyRecoilRotation();
        }

        private void ApplyRecoilRotation()
        {
            if (_recoilTarget == null) return;

            float recoilFactor = Mathf.Clamp01(_amount);
            float recoilAngle = _settings.RecoilAngle * recoilFactor;
            _recoilTarget.localRotation = _initialLocalRotation * Quaternion.Euler(-recoilAngle, 0f, 0f);
        }

        private void ResetTargetRotation()
        {
            if (_recoilTarget != null)
            {
                _recoilTarget.localRotation = _initialLocalRotation;
            }
        }

        private void SetConstraintWeight(float weight)
        {
            if (_recoilConstraint != null)
            {
                _recoilConstraint.weight = weight;
            }
        }

        private void OnDisable()
        {
            ResetMotion();
        }
    }
}
