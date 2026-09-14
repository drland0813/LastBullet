using UnityEngine;

namespace LastBullet
{
    public class SingleShootStrategy : IShootStrategy
    {
        public void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                            ObjectPool<BulletProjectile> pool, float damage,
                            GameObject hitImpactPrefab, GameObject instigator)
        {
            if (pool == null)
            {
                Debug.LogError("[SingleShootStrategy] Pool is null. Make sure the data asset has a " +
                               "BulletPrefab that is registered in the BulletObjectPoolManager.");
                return;
            }

            Vector3 spreadDirection = ApplySpread(direction, data.Spread);

            BulletProjectile bullet = pool.Get();
            bullet.Launch(origin, spreadDirection, data.BulletSpeed, damage, data.HitForce,
                          data.BulletLifetime, hitImpactPrefab, pool, instigator);
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
