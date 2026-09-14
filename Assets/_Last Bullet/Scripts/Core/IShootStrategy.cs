using UnityEngine;

namespace LastBullet
{
    public interface IShootStrategy
    {
        void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                     ObjectPool<BulletProjectile> pool, float damage,
                     GameObject hitImpactPrefab, GameObject instigator);
    }
}
