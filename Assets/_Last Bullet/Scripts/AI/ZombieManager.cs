using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet
{
    [Serializable]
    public class ZombieSpawnEntry
    {
        public ZombieAI Prefab;
        [Min(0f)] public float Weight = 1f;
    }

    public class ZombieManager : Singleton<ZombieManager>
    {
        [Header("Pool")]
        [SerializeField] private List<ZombieSpawnEntry> _spawnEntries = new List<ZombieSpawnEntry>();
        [SerializeField] [Min(1)] private int _maxAlive = 12;
        [SerializeField] [Min(0)] private int _prewarmPerEntry = 6;

        [Header("Spawn")]
        [SerializeField] [Min(0.1f)] private float _spawnInterval = 2f;
        [SerializeField] [Min(0f)] private float _spawnMinRadius = 12f;
        [SerializeField] [Min(0f)] private float _spawnMaxRadius = 25f;
        [SerializeField] [Min(1)] private int _spawnRetryCount = 10;
        [SerializeField] [Min(0f)] private float _viewMargin = 2f;
        [SerializeField] [Min(0.1f)] private float _navMeshSampleDistance = 2f;

        [Header("Despawn")]
        [SerializeField] [Min(0f)] private float _despawnDistance = 60f;
        [SerializeField] [Min(0.1f)] private float _despawnCheckInterval = 2f;

        [Header("Spawn Clearance")]
        [Tooltip("Reject spawn points boxed in by walls on most sides (e.g. inside buildings).")]
        [SerializeField] [Min(0f)] private float _spawnClearanceRadius = 2f;
        [SerializeField] [Min(1)] private int _spawnClearanceRays = 8;
        [SerializeField] [Range(0f, 1f)] private float _spawnBlockedThreshold = 0.75f;

        private Dictionary<ZombieAI, ObjectPool<ZombieAI>> _pools = new Dictionary<ZombieAI, ObjectPool<ZombieAI>>();
        private Dictionary<ZombieAI, ObjectPool<ZombieAI>> _poolByInstance = new Dictionary<ZombieAI, ObjectPool<ZombieAI>>();
        private List<ZombieAI> _activeZombies = new List<ZombieAI>();
        private float _spawnTimer;
        private float _despawnTimer;
        private Transform _playerTransform;
        private Camera _mainCamera;
        private bool _spawningEnabled = true;
        private float _spawnSpeedMultiplier = 1f;
        private float _spawnHealthMultiplier = 1f;

        public int ActiveCount => _activeZombies.Count;
        public int MaxAlive => _maxAlive;
        public event Action<ZombieAI> OnZombieSpawned;
        public event Action<ZombieAI> OnZombieDespawned;
        public event Action<ZombieAI> OnZombieKilled;

        public void StopSpawning()
        {
            _spawningEnabled = false;
        }

        public void ResumeSpawning()
        {
            _spawningEnabled = true;
            _spawnTimer = 0f;
        }

        public void SetDifficulty(float spawnInterval, int maxAlive, float speedMultiplier, float healthMultiplier)
        {
            _spawnInterval = Mathf.Max(0.1f, spawnInterval);
            _maxAlive = Mathf.Max(1, maxAlive);
            _spawnSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            _spawnHealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
        }

        protected override void Awake()
        {
            base.Awake();
            BuildPools();
        }

        private void Start()
        {
            CachePlayerAndCamera();
            PrewarmPools();
        }

        private void Update()
        {
            if (_playerTransform == null || _mainCamera == null)
            {
                CachePlayerAndCamera();
                if (_playerTransform == null) return;
            }

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                TrySpawn();
            }

            _despawnTimer += Time.deltaTime;
            if (_despawnTimer >= _despawnCheckInterval)
            {
                _despawnTimer = 0f;
                CheckDespawn();
            }
        }

        private void BuildPools()
        {
            _pools.Clear();

            foreach (ZombieSpawnEntry entry in _spawnEntries)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0f) continue;
                if (_pools.ContainsKey(entry.Prefab)) continue;
                _pools.Add(entry.Prefab, new ObjectPool<ZombieAI>(entry.Prefab));
            }
        }

        private void PrewarmPools()
        {
            if (_prewarmPerEntry <= 0) return;

            foreach (KeyValuePair<ZombieAI, ObjectPool<ZombieAI>> pair in _pools)
            {
                for (int i = 0; i < _prewarmPerEntry; i++)
                {
                    ZombieAI instance = pair.Value.GetInactive();
                    RegisterInstance(instance, pair.Value);
                    pair.Value.Store(instance);
                }
            }
        }

        private void TrySpawn()
        {
            if (!_spawningEnabled) return;
            if (_activeZombies.Count >= _maxAlive) return;
            if (_pools.Count == 0 || _playerTransform == null) return;

            ZombieSpawnEntry entry = PickEntry();
            if (entry == null || entry.Prefab == null) return;
            if (!_pools.TryGetValue(entry.Prefab, out ObjectPool<ZombieAI> pool) || pool == null) return;

            if (!PickSpawnPosition(out Vector3 spawnPosition)) return;

            ZombieAI zombie = pool.Get();
            RegisterInstance(zombie, pool);
            zombie.OnDespawnReady -= HandleZombieDespawnReady;
            zombie.OnDespawnReady += HandleZombieDespawnReady;
            zombie.OnKilled -= HandleZombieKilled;
            zombie.OnKilled += HandleZombieKilled;
            _activeZombies.Add(zombie);
            zombie.SetDifficultyMultipliers(_spawnSpeedMultiplier, _spawnHealthMultiplier);
            zombie.SpawnAt(spawnPosition);
            OnZombieSpawned?.Invoke(zombie);
        }

        private void CheckDespawn()
        {
            if (_playerTransform == null || _activeZombies.Count == 0) return;

            Vector3 playerPosition = _playerTransform.position;

            for (int i = _activeZombies.Count - 1; i >= 0; i--)
            {
                ZombieAI zombie = _activeZombies[i];
                if (zombie == null)
                {
                    _activeZombies.RemoveAt(i);
                    continue;
                }

                if (zombie.IsDead) continue;

                float distance = Vector3.Distance(zombie.transform.position, playerPosition);
                if (distance <= _despawnDistance) continue;
                if (IsInPlayerView(zombie.transform.position)) continue;

                StoreZombie(zombie);
            }
        }

        private void HandleZombieDespawnReady(ZombieAI zombie)
        {
            StoreZombie(zombie);
        }

        private void HandleZombieKilled(ZombieAI zombie)
        {
            OnZombieKilled?.Invoke(zombie);
        }

        private void StoreZombie(ZombieAI zombie)
        {
            if (zombie == null) return;

            _activeZombies.Remove(zombie);
            zombie.OnDespawnReady -= HandleZombieDespawnReady;
            zombie.OnKilled -= HandleZombieKilled;

            if (_poolByInstance.TryGetValue(zombie, out ObjectPool<ZombieAI> pool) && pool != null)
            {
                pool.Store(zombie);
            }
            else
            {
                Destroy(zombie.gameObject);
            }

            OnZombieDespawned?.Invoke(zombie);
        }

        private void RegisterInstance(ZombieAI instance, ObjectPool<ZombieAI> pool)
        {
            if (instance == null || pool == null) return;
            if (!_poolByInstance.ContainsKey(instance))
            {
                _poolByInstance.Add(instance, pool);
            }
        }

        private ZombieSpawnEntry PickEntry()
        {
            float totalWeight = 0f;
            foreach (ZombieSpawnEntry entry in _spawnEntries)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0f) continue;
                if (!_pools.ContainsKey(entry.Prefab)) continue;
                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (ZombieSpawnEntry entry in _spawnEntries)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0f) continue;
                if (!_pools.ContainsKey(entry.Prefab)) continue;
                roll -= entry.Weight;
                if (roll <= 0f) return entry;
            }

            return null;
        }

        private bool PickSpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = Vector3.zero;
            if (_playerTransform == null) return false;

            float maxRadius = Mathf.Max(_spawnMaxRadius, _spawnMinRadius);
            Vector3 playerPosition = _playerTransform.position;

            for (int attempt = 0; attempt < _spawnRetryCount; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = UnityEngine.Random.Range(_spawnMinRadius, maxRadius);
                Vector3 candidate = playerPosition + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, _navMeshSampleDistance, NavMesh.AllAreas)) continue;

                if (Vector3.Distance(hit.position, playerPosition) < _spawnMinRadius) continue;
                if (IsInPlayerView(hit.position)) continue;
                if (IsSpawnBlocked(hit.position)) continue;

                spawnPosition = hit.position;
                return true;
            }

            return false;
        }

        private bool IsInPlayerView(Vector3 position)
        {
            if (_mainCamera == null) return false;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
            Bounds bounds = new Bounds(position + Vector3.up * 1f, Vector3.one * (_viewMargin + 1f));
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        private bool IsSpawnBlocked(Vector3 position)
        {
            if (_spawnClearanceRays <= 0 || _spawnClearanceRadius <= 0f) return false;

            Vector3 origin = position + Vector3.up * 1f;
            int blockedCount = 0;

            for (int i = 0; i < _spawnClearanceRays; i++)
            {
                float angle = (float)i / _spawnClearanceRays * Mathf.PI * 2f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                if (Physics.Raycast(origin, direction, out RaycastHit hitInfo, _spawnClearanceRadius)
                    && !IsCharacterCollider(hitInfo.collider))
                {
                    blockedCount++;
                }
            }

            return (float)blockedCount / _spawnClearanceRays >= _spawnBlockedThreshold;
        }

        private bool IsCharacterCollider(Collider hitCollider)
        {
            if (hitCollider == null) return false;
            if (hitCollider.GetComponentInParent<ZombieAI>() != null) return true;
            return hitCollider.GetComponentInParent<TopDownThirdPersonController>() != null;
        }

        private void CachePlayerAndCamera()
        {
            if (_playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                }
                else
                {
                    TopDownThirdPersonController player = FindFirstObjectByType<TopDownThirdPersonController>();
                    if (player != null)
                    {
                        _playerTransform = player.transform;
                    }
                }
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }
    }
}
