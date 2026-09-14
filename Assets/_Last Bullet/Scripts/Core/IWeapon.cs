using System;
using UnityEngine;

namespace LastBullet
{
    public interface IWeapon
    {
        WeaponDataBase Data { get; }
        bool CanFire { get; }
        bool IsReloading { get; }
        int CurrentAmmo { get; }
        int ReserveAmmo { get; }

        void Fire(Vector3 origin, Vector3 direction);
        void ReleaseFire();
        void Reload();
        void OnEquip();
        void OnUnequip();
        Transform GetTransform();
        Transform GetLeftHandTransform();
        Transform GetRightHandTransform();
        event Action<int, int> OnAmmoChanged;
        event Action OnFirePerformed;
        event Action OnReloadStarted;
        event Action OnReloadFinished;
        Transform GetFirePoint();
    }
}
