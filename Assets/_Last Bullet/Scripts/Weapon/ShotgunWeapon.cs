using UnityEngine;

namespace LastBullet
{
    public class ShotgunWeapon : WeaponBase
    {
        private IHitscanStrategy _shootStrategy;

        protected override void Awake()
        {
            base.Awake();

            ShotgunDataSO shotgunData = _data as ShotgunDataSO;
            if (shotgunData != null)
            {
                _shootStrategy = new HitscanSpreadShootStrategy(shotgunData.PelletCount,
                                                                shotgunData.SpreadAngle);
            }
            else
            {
                Debug.LogError($"[ShotgunWeapon] {name}: data must be ShotgunDataSO!", this);
                _shootStrategy = new HitscanSpreadShootStrategy(8, 15f);
            }
        }

        protected override void ShootInternal(Vector3 origin, Vector3 direction)
        {
            _shootStrategy.Execute(origin, direction, _data, gameObject);
        }
    }
}
