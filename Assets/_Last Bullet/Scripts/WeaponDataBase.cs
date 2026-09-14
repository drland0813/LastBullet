using UnityEngine;

namespace LastBullet
{
    public abstract class WeaponDataBase : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string Name;
        public Sprite Icon;

        [Header("Stats")]
        public float Damage;
        public float FireRate;
        public int MagazineSize;
        [Tooltip("Total bullets carried including the loaded magazine. 0 reloads when depleted.")]
        public int TotalAmmo = 100;
        public float Range;

        [Header("FX")]
        public GameObject MuzzleFlashPrefab;
        public GameObject HitImpactPrefab;

        [Header("Audio")]
        public AudioClip FireSound;
        public AudioClip ReloadSound;
        public AudioClip EmptySound;

        [Header("Recoil")]
        public float RecoilImpulseForce;

        [Header("Hit Reaction")]
        [Tooltip("Impulse applied to ragdoll on hit/kill. Per-weapon tuning.")]
        public float HitForce = 10f;
    }
}


