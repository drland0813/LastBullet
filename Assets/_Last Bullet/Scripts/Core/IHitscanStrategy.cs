using UnityEngine;

namespace LastBullet
{
    public interface IHitscanStrategy
    {
        void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                     GameObject instigator);
    }
}