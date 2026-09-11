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

        [Header("Hitscan")]
        [Tooltip("Visual-only tracer prefab spawned for hitscan shots. Needs a BulletTracer " +
                 "component + TrailRenderer (BulletTracer material). No collider is " +
                 "attached, so it's cheap.")]
        public GameObject TracerPrefab;
        [Tooltip("How long the hitscan tracer line stays visible in seconds")]
        public float TracerLifetime = 0.06f;

        [Header("Reload")]
        public float ReloadTime = 1.5f;

        [Header("Accuracy")]
        [Tooltip("Maximum random spread angle in degrees")]
        public float Spread = 1f;

        [Header("Fire Mode")]
        public FireMode FireMode = FireMode.SemiAuto;
    }
}
