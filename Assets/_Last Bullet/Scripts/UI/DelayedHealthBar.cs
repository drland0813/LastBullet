using UnityEngine;
using UnityEngine.UI;

namespace LastBullet
{
    public class DelayedHealthBar : MonoBehaviour
    {
        [Header("Layers")]
        [Tooltip("Top layer. Follows value instantly.")]
        [SerializeField] private Image _frontImage;
        [Tooltip("Bottom layer. Waits a moment then drains toward the front layer.")]
        [SerializeField] private Image _backImage;

        [Header("Trail")]
        [SerializeField] [Min(0f)] private float _trailDelay = 0.4f;
        [SerializeField] [Min(0f)] private float _trailSpeed = 0.6f;

        private float _trailTimer;

        private void Update()
        {
            if (_backImage == null || _frontImage == null) return;
            if (_trailTimer > 0f)
            {
                _trailTimer -= Time.deltaTime;
                return;
            }

            _backImage.fillAmount = Mathf.MoveTowards(
                _backImage.fillAmount,
                _frontImage.fillAmount,
                _trailSpeed * Time.deltaTime);
        }

        public void SetValue(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (_frontImage != null)
            {
                if (normalized < _frontImage.fillAmount && _backImage != null)
                {
                    _trailTimer = _trailDelay;
                }
                _frontImage.fillAmount = normalized;
            }

            if (_backImage != null && normalized >= _backImage.fillAmount)
            {
                _backImage.fillAmount = normalized;
                _trailTimer = 0f;
            }
        }

        public void SnapTo(float current, float max)
        {
            float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (_frontImage != null)
            {
                _frontImage.fillAmount = normalized;
            }

            if (_backImage != null)
            {
                _backImage.fillAmount = normalized;
            }

            _trailTimer = 0f;
        }
    }
}
