using UnityEngine;

namespace LastBullet
{
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject instigator);
    }
}
