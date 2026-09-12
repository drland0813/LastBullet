using UnityEngine;

namespace LastBullet
{
    public class GunRecoilMotion : MonoBehaviour
    {
        [Header("Recoil")]
        [SerializeField] private float _recoilDistance = 0.05f;
        [SerializeField] private float _recoilAngle = 5f;
        [SerializeField] private float _recoilSpeed = 25f;
        [SerializeField] private float _returnSpeed = 15f;

        private Vector3 _initialLocalPosition;
        private Quaternion _initialLocalRotation;

        private float _recoilAmount;

        private void Awake()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
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

            transform.localPosition =
                _initialLocalPosition +
                Vector3.back * (_recoilDistance * _recoilAmount);

            transform.localRotation =
                _initialLocalRotation *
                Quaternion.Euler(
                    -_recoilAngle * _recoilAmount,
                    0f,
                    0f
                );
        }

        public void Play()
        {
            _initialLocalPosition = transform.localPosition;
            _initialLocalRotation = transform.localRotation;
            _recoilAmount = Mathf.Min(_recoilAmount + 1f, 1.5f);
        }

        public void ResetMotion()
        {
            _recoilAmount = 0f;

            transform.localPosition = _initialLocalPosition;
            transform.localRotation = _initialLocalRotation;
        }
    }
}