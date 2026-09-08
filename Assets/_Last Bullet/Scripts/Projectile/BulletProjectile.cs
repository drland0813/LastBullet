using UnityEngine;

namespace LastBullet
{
    public class BulletProjectile : ProjectileBase
    {
        protected override void OnHit(Collider other)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(_damage, gameObject);
            }
        }
    }
}
