using UnityEngine;

namespace LastBullet
{
    [RequireComponent(typeof(TrailRenderer))]
    public class BulletTracer : MonoBehaviour
    {
        [Header("Tracer")]
        [Tooltip("Seconds the trail stays visible when the caller passes 0")]
        [SerializeField] private float _defaultLifetime = 0.06f;

        private TrailRenderer _trail;
        private float _timer;
        private bool _isActive;

        private Vector3 _from;
        private Vector3 _to;
        private float _duration;
        private ObjectPool<BulletTracer> _pool;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            _trail.time = _defaultLifetime;
            gameObject.SetActive(false);
        }

        public void SetPool(ObjectPool<BulletTracer> pool)
        {
            if(pool != null && _pool != pool)
            {
                _pool = pool;
            }
        }

        public void Show(Vector3 from, Vector3 to, float lifetime)
        {
            _duration = lifetime > 0f ? lifetime : _defaultLifetime;

            _trail.time = _duration;
            _trail.emitting = false;
            transform.position = from;
            _trail.Clear();

            _from = from;
            _to = to;
            _timer = _duration;

            _isActive = true;
            gameObject.SetActive(true);
            _trail.emitting = true;
        }

        private void Update()
        {
            if (!_isActive) return;

            _timer -= Time.deltaTime;
            float t = 1f - Mathf.Max(0f, _timer / _duration);
            transform.position = Vector3.Lerp(_from, _to, t);

            if (_timer <= 0f)
            {
                _trail.emitting = false;
                _trail.Clear();
                _isActive = false;
                _pool?.Store(this);
            }
        }
    }
}