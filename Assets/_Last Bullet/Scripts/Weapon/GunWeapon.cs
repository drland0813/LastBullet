using UnityEngine;

namespace LastBullet
{
    public class GunWeapon : WeaponBase
    {
        private IHitscanStrategy _shootStrategy;

        protected override void Awake()
        {
            base.Awake();
            _shootStrategy = new HitscanShootStrategy();
        }

        protected override void ShootInternal(Vector3 origin, Vector3 direction)
        {
            _shootStrategy.Execute(origin, direction, _data, gameObject);
        }
    }
}
