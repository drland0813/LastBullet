using UnityEngine;

namespace LastBullet
{
    [CreateAssetMenu(fileName = "NewShotgunData", menuName = "Last Bullet/Weapon/Shotgun Data")]
    public class ShotgunDataSO : GunDataSO
    {
        [Header("Shotgun")]
        [Tooltip("Number of pellets per shot")]
        public int PelletCount = 8;

        [Tooltip("Cone spread angle in degrees for pellets")]
        public float SpreadAngle = 15f;
    }
}
