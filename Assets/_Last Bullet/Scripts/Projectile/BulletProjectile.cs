using UnityEngine;

namespace LastBullet
{
    public class BulletProjectile : ProjectileBase
    {
        protected override void OnHit(Collider other, HitData hit)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(hit);
            }
        }
    }
}
