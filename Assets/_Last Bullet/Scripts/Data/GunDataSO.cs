using UnityEngine;

namespace LastBullet
{
    public enum FireMode
    {
        SemiAuto,   
        FullAuto  
    }

    [CreateAssetMenu(fileName = "NewGunData", menuName = "Last Bullet/Weapon/Gun Data")]
    public class GunDataSO : WeaponDataBase
    {
        [Header("Projectile")]
        public BulletProjectile BulletPrefab;
        public float BulletSpeed = 30f;
        public float BulletLifetime = 3f;

        [Header("Reload")]
        public float ReloadTime = 1.5f;

        [Header("Accuracy")]
        [Tooltip("Maximum random spread angle in degrees")]
        public float Spread = 1f;

        [Header("Fire Mode")]
        public FireMode FireMode = FireMode.SemiAuto;
    }
}
