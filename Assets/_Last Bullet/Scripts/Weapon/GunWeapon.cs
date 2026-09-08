using UnityEngine;

namespace LastBullet
{
    public class GunWeapon : WeaponBase
    {
        private IShootStrategy _shootStrategy;

        protected override void Awake()
        {
            base.Awake();
            _shootStrategy = new SingleShootStrategy();
        }

        protected override void ShootInternal(Vector3 origin, Vector3 direction,
                                              ObjectPool<BulletProjectile> pool)
        {
            _shootStrategy.Execute(origin, direction, _data, pool,
                                   _data.Damage, _data.HitImpactPrefab);
        }
    }
}
