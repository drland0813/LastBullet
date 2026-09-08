using UnityEngine;

namespace LastBullet
{
    public class SpreadShootStrategy : IShootStrategy
    {
        private int _pelletCount;
        private float _spreadAngle;

        public SpreadShootStrategy(int pelletCount, float spreadAngle)
        {
            _pelletCount = pelletCount;
            _spreadAngle = spreadAngle;
        }

        public void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                            ObjectPool<BulletProjectile> pool, float damage, GameObject hitImpactPrefab)
        {
            float damagePerPellet = damage / _pelletCount;

            for (int i = 0; i < _pelletCount; i++)
            {
                Vector3 pelletDirection = ApplySpread(direction, _spreadAngle);

                BulletProjectile bullet = pool.Get();
                bullet.Launch(origin, pelletDirection, data.BulletSpeed, damagePerPellet,
                              data.BulletLifetime, hitImpactPrefab, pool);
            }
        }

        private Vector3 ApplySpread(Vector3 direction, float spreadAngle)
        {
            float halfAngle = spreadAngle * 0.5f;
            float randomYaw = Random.Range(-halfAngle, halfAngle);
            float randomPitch = Random.Range(-halfAngle, halfAngle);

            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);
            return spreadRotation * direction;
        }
    }
}
