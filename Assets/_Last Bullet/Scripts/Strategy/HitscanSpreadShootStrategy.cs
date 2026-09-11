using UnityEngine;

namespace LastBullet
{
    public class HitscanSpreadShootStrategy : HitscanShootStrategy
    {
        private int _pelletCount;
        private float _spreadAngle;

        public HitscanSpreadShootStrategy(int pelletCount, float spreadAngle)
        {
            _pelletCount = Mathf.Max(1, pelletCount);
            _spreadAngle = Mathf.Max(0f, spreadAngle);
        }

        public void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                            GameObject instigator)
        {
            float damagePerPellet = data.Damage / _pelletCount;

            for (int i = 0; i < _pelletCount; i++)
            {
                Vector3 pelletDirection = ApplySpread(direction, _spreadAngle);
                FireRay(origin, pelletDirection, data, damagePerPellet, instigator);
            }
        }
    }
}