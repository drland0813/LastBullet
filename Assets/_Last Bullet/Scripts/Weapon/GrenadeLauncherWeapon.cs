using UnityEngine;

namespace LastBullet
{
    public class GrenadeLauncherWeapon : WeaponBase
    {
        private IShootStrategy _shootStrategy;

        protected override void Awake()
        {
            base.Awake();
            _shootStrategy = new SingleShootStrategy();
        }

        protected override void ShootInternal(Vector3 origin, Vector3 direction)
        {
            _shootStrategy.Execute(origin, direction, _data, _bulletPool,
                                   _data.Damage, _data.HitImpactPrefab);
        }
    }
}