using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "Last Bullet/Weapon Database")]
    public class WeaponDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<WeaponBase> _weaponPrefabs = new List<WeaponBase>();

        public WeaponBase GetPrefabById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (WeaponBase weaponPrefab in _weaponPrefabs)
            {
                if (weaponPrefab == null)
                {
                    continue;
                }

                if (weaponPrefab.Data != null && weaponPrefab.Data.Id == id)
                {
                    return weaponPrefab;
                }
            }

            return null;
        }
    }
}
