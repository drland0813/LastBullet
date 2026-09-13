using UnityEngine;

namespace LastBullet
{
    public class GunRecoilMotion : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _recoilTarget;

        [Header("Recoil")]
        [SerializeField] private float _recoilDistance = 0.05f;
        [SerializeField] private float _recoilAngle = 5f;
        [SerializeField] private float _recoilSpeed = 25f;
        [SerializeField] private float _returnSpeed = 15f;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;

        private float _recoilAmount;

        private Transform RecoilTarget => _recoilTarget != null ? _recoilTarget : transform;

        private void Awake()
        {
            _initialLocalPosition = RecoilTarget.localPosition;
            _initialLocalRotation = RecoilTarget.localRotation;
        }

        private void LateUpdate()
        {
            if (_recoilAmount <= 0.001f)
            {
                _recoilAmount = 0f;
                return;
            }

            _recoilAmount = Mathf.Lerp(
                _recoilAmount,
                0f,
                _returnSpeed * Time.deltaTime
            );

            RecoilTarget.localPosition =
                _initialLocalPosition +
                Vector3.back * (_recoilDistance * _recoilAmount);

            RecoilTarget.localRotation =
                _initialLocalRotation *
                Quaternion.Euler(
                    -_recoilAngle * _recoilAmount,
                    0f,
                    0f
                );
        }

        public void Play()
        {
            _initialLocalPosition = RecoilTarget.localPosition;
            _initialLocalRotation = RecoilTarget.localRotation;
            _recoilAmount = Mathf.Min(_recoilAmount + 1f, 1.5f);
        }

        public void ResetMotion()
        {
            _recoilAmount = 0f;

            RecoilTarget.localPosition = _initialLocalPosition;
            RecoilTarget.localRotation = _initialLocalRotation;
        }
    }
}