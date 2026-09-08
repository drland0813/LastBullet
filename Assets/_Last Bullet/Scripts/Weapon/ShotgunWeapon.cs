using UnityEngine;

namespace LastBullet
{
    public class ShotgunWeapon : WeaponBase
    {
        private IShootStrategy _shootStrategy;

        protected override void Awake()
        {
            base.Awake();

            ShotgunDataSO shotgunData = _data as ShotgunDataSO;
            if (shotgunData != null)
            {
                _shootStrategy = new SpreadShootStrategy(shotgunData.PelletCount,
                                                         shotgunData.SpreadAngle);
            }
            else
            {
                Debug.LogError($"[ShotgunWeapon] {name}: data must be ShotgunDataSO!", this);
                _shootStrategy = new SpreadShootStrategy(8, 15f);
            }
        }

        protected override void ShootInternal(Vector3 origin, Vector3 direction,
                                              ObjectPool<BulletProjectile> pool)
        {
            _shootStrategy.Execute(origin, direction, _data, pool,
                                   _data.Damage, _data.HitImpactPrefab);
        }
    }
}
