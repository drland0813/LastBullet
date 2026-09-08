using UnityEngine;

namespace LastBullet
{
    public class SingleShootStrategy : IShootStrategy
    {
        public void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                            ObjectPool<BulletProjectile> pool, float damage, GameObject hitImpactPrefab)
        {
            Vector3 spreadDirection = ApplySpread(direction, data.Spread);

            BulletProjectile bullet = pool.Get();
            bullet.Launch(origin, spreadDirection, data.BulletSpeed, damage,
                          data.BulletLifetime, hitImpactPrefab, pool);
        }

        private Vector3 ApplySpread(Vector3 direction, float spread)
        {
            if (spread <= 0f) return direction;

            float halfSpread = spread * 0.5f;
            float randomYaw = Random.Range(-halfSpread, halfSpread);
            float randomPitch = Random.Range(-halfSpread, halfSpread);

            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);
            return spreadRotation * direction;
        }
    }
}
