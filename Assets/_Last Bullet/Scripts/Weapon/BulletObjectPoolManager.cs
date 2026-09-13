using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    public class BulletObjectPoolManager : Singleton<BulletObjectPoolManager>
    {
        [Header("Bullet Samples")]
        [SerializeField] private List<BulletTracer> _bulletSamples = new();
        [SerializeField] private MuzzleFlash _muzzlePrefab;
        private ObjectPool<MuzzleFlash> _muzzlePool;
        private readonly Dictionary<string, ObjectPool<BulletTracer>> _pools = new();
        private readonly Dictionary<string, Transform> _poolRoots = new();
        private ObjectPool<BulletTracer> _gunProjectilePool;

        protected override void Awake()
        {
            base.Awake();
            InitAllPools();
        }

        private void InitAllPools()
        {
            foreach (BulletTracer sample in _bulletSamples)
            {
                if (sample == null) continue;

                string key = sample.name;

                if (_pools.ContainsKey(key))
                {
                    Debug.LogWarning($"[BulletObjectPoolManager] Duplicate sample name: '{key}'. Skipping.", this);
                    continue;
                }
                ObjectPool<BulletTracer> pool = new ObjectPool<BulletTracer>(sample);
                Debug.Log($"Init pool: {key}");
                _pools.Add(key, pool);
                _poolRoots.Add(key, transform);
            }
            _muzzlePool = new ObjectPool<MuzzleFlash>(_muzzlePrefab);
        }

        public ObjectPool<BulletTracer> GetBulletPool(string bulletName)
        {
            if (_pools.TryGetValue(bulletName, out ObjectPool<BulletTracer> pool))
            {
                return pool;
            }

            Debug.LogError($"[BulletObjectPoolManager] No pool found for bullet: '{bulletName}'. " +
                           $"Make sure it's added to the Bullet Samples list.", this);
            return null;
        }
        public ObjectPool<BulletTracer> GetBulletPool(BulletTracer bulletPrefab)
        {
            if (bulletPrefab == null)
            {
                Debug.LogError("[BulletObjectPoolManager] bulletPrefab is null.", this);
                return null;
            }

            return GetBulletPool(bulletPrefab.name);
        }

        public ObjectPool<MuzzleFlash> GetMuzzleFlashPool()
        {
            return _muzzlePool;
        }

        public Transform GetPoolRoot(string bulletName)
        {
            if (_poolRoots.TryGetValue(bulletName, out Transform root))
            {
                return root;
            }

            return transform;
        }
    }
}
