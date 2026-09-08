using UnityEngine;

namespace LastBullet
{
    public class WeaponController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _firePoint;
        [SerializeField] private AimController _aimController;

        [Header("Starting Weapon")]
        [SerializeField] private WeaponBase _startingWeapon;

        private PlayerInputs _input;
        private IWeapon _currentWeapon;

        private void Start()
        {
            _input = InputManager.Instance.PlayerInputs;

            if (_startingWeapon != null)
            {
                EquipWeapon(_startingWeapon);
            }
        }

        private void Update()
        {
            if (_currentWeapon == null) return;

            HandleFire();
            HandleReload();
        }

        private void HandleFire()
        {
            if (_input.fire)
            {
                Vector3 aimDirection = _aimController != null
                    ? _aimController.GetAimDirection()
                    : _firePoint.forward;

                Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;

                _currentWeapon.Fire(origin, aimDirection);
            }
            else
            {
                _currentWeapon.ReleaseFire();
            }
        }

        private void HandleReload()
        {
            if (!_input.reload) return;

            _currentWeapon.Reload();
            _input.reload = false;
        }

        public void EquipWeapon(WeaponBase newWeapon)
        {
            if (_currentWeapon != null)
            {
                _currentWeapon.OnAmmoChanged -= HandleAmmoChanged;
                _currentWeapon.OnReloadStarted -= HandleReloadStarted;
                _currentWeapon.OnReloadFinished -= HandleReloadFinished;
                _currentWeapon.OnUnequip();
            }

            _currentWeapon = newWeapon;
            _currentWeapon.OnEquip();

            _currentWeapon.OnAmmoChanged += HandleAmmoChanged;
            _currentWeapon.OnReloadStarted += HandleReloadStarted;
            _currentWeapon.OnReloadFinished += HandleReloadFinished;
        }

        public void UnequipCurrentWeapon()
        {
            if (_currentWeapon == null) return;

            _currentWeapon.OnAmmoChanged -= HandleAmmoChanged;
            _currentWeapon.OnReloadStarted -= HandleReloadStarted;
            _currentWeapon.OnReloadFinished -= HandleReloadFinished;

            _currentWeapon.OnUnequip();
            _currentWeapon = null;
        }

        private void HandleAmmoChanged(int current, int max)
        {
            Debug.Log($"[WeaponController] Ammo: {current}/{max}");
        }

        private void HandleReloadStarted()
        {
            Debug.Log("[WeaponController] Reloading...");
        }

        private void HandleReloadFinished()
        {
            Debug.Log("[WeaponController] Reload complete!");
        }
    }
}
