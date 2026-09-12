using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LastBullet
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private AimController _aimController;

        [Header("Database")]
        [SerializeField] private WeaponDatabaseSO _weaponDatabase;

        [Header("Starting Weapon")]
        [SerializeField] private WeaponBase _startingWeapon;
        [SerializeField] private string _startingWeaponId;

        [Header("Rigging")] 
        [SerializeField] private Transform _equipPos;
        [SerializeField] private Transform _aimPos;
        [SerializeField] private TwoBoneIKConstraint _leftHandIKConstraint;
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private TwoBoneIKConstraint _rightHandIKConstraint;
        [SerializeField] private Transform _rightHandTarget;
        [SerializeField] private Transform _leftHandIKPos;
        [SerializeField] private Transform _rightHandIKPos;
        private Transform _firePoint;
        public bool CanFire;

        [Header("Testing")]
        [SerializeField] private bool _forceFiring;
        [SerializeField] private float _aimTransitionDuration = 0.05f;
        public bool ForceFiring
        {
            get => _forceFiring;
            set => _forceFiring = value;
        }
        
        private PlayerInputs _input;
        private IWeapon _currentWeapon;
        private Transform _currentWeaponTransform;
        private bool _isFiring = false;
        
        public Action OnFirePerformed;
        public Action OnFireStarted;
        public WeaponBase CurrentWeapon => _currentWeapon as WeaponBase;

        private void Start()
        {
            _input = InputManager.Instance.PlayerInputs;

            // if (!string.IsNullOrEmpty(_startingWeaponId))
            // {
            //     EquipWeaponById(_startingWeaponId);
            // }

        }

        private void Update()
        {
            if (_currentWeapon == null) return;

            HandleFire();
            HandleReload();

        }

        private void HandleFire()
        {
            bool isFiring = _input.fire || ForceFiring;
            bool wasFiring = _isFiring;
            _isFiring = isFiring;

            if (isFiring && !wasFiring)
            {
                OnFireStarted?.Invoke();
            }

            if (isFiring)
            {
                UpdateWeaponTransform();
                Vector3 aimDirection = _aimController != null
                    ? _aimController.GetAimDirection()
                    : _firePoint.forward;

                Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;
                _currentWeapon.Fire(origin, aimDirection);
            }
            else
            {
                ApplyWeaponPose(_equipPos, false);
                
                _currentWeapon.ReleaseFire();
            }
        }

        private void HandleReload()
        {
            if (!_input.reload) return;

            _currentWeapon.Reload();
            _input.reload = false;
        }

        public void EquipWeapon()
        {
            if (!string.IsNullOrEmpty(_startingWeaponId))
            {
                EquipWeaponById(_startingWeaponId);
                return;
            }

            EquipWeapon(_startingWeapon);
        }

        public void EquipWeaponById(string weaponId)
        {
            if (_weaponDatabase == null)
            {
                Debug.LogError("[WeaponController] WeaponDatabaseSO is missing.");
                return;
            }
            
            WeaponBase weaponPrefab = _weaponDatabase.GetPrefabById(weaponId);
            if (weaponPrefab == null)
            {
                Debug.LogError($"[WeaponController] No weapon data found for id: {weaponId}");
                return;
            }

            WeaponBase weapon = Instantiate(weaponPrefab, transform);
            if (weapon == null)
            {
                Debug.LogError($"[WeaponController] Prefab '{weaponPrefab.name}' does not contain WeaponBase.");
                Destroy(weapon.gameObject);
                return;
            }

            EquipWeapon(weapon);
        }

        public void EquipWeapon(WeaponBase newWeapon)
        {
            if (_currentWeapon == newWeapon) return;

            if (_currentWeapon != null)
            {
                _currentWeapon.OnAmmoChanged -= HandleAmmoChanged;
                _currentWeapon.OnFirePerformed -= HandleWeaponFirePerformed;
                _currentWeapon.OnReloadStarted -= HandleReloadStarted;
                _currentWeapon.OnReloadFinished -= HandleReloadFinished;
                _currentWeapon.OnUnequip();
            }

            _currentWeapon = newWeapon;
            _currentWeapon.OnEquip();
            _leftHandIKPos = _currentWeapon.GetLeftHandTransform();
            _rightHandIKPos = _currentWeapon.GetRightHandTransform();
            _firePoint = _currentWeapon.GetFirePoint();
            _currentWeapon.OnAmmoChanged += HandleAmmoChanged;
            _currentWeapon.OnFirePerformed += HandleWeaponFirePerformed;
            _currentWeapon.OnReloadStarted += HandleReloadStarted;
            _currentWeapon.OnReloadFinished += HandleReloadFinished;
            _currentWeaponTransform = _currentWeapon.GetTransform();

            ApplyWeaponSocketPoses();
            UpdateWeaponTransform();
        }

        public void UnequipCurrentWeapon()
        {
            if (_currentWeapon == null) return;

            _currentWeapon.OnAmmoChanged -= HandleAmmoChanged;
            _currentWeapon.OnFirePerformed -= HandleWeaponFirePerformed;
            _currentWeapon.OnReloadStarted -= HandleReloadStarted;
            _currentWeapon.OnReloadFinished -= HandleReloadFinished;

            _currentWeapon.OnUnequip();
            Destroy(_currentWeaponTransform.gameObject);
            _currentWeapon = null;
            _currentWeaponTransform = null;
            _leftHandIKPos = null;
            _rightHandIKPos = null;
        }

        private void HandleAmmoChanged(int current, int max)
        {
            Debug.Log($"[WeaponController] Ammo: {current}/{max}");
        }

        private void HandleWeaponFirePerformed()
        {
            OnFirePerformed?.Invoke();
        }

        private void HandleReloadStarted()
        {
            Debug.Log("[WeaponController] Reloading...");
        }

        private void HandleReloadFinished()
        {
            Debug.Log("[WeaponController] Reload complete!");
        }

        private void UpdateWeaponTransform()
        {

            if (_isFiring)
            {
                ApplyWeaponPose(_aimPos, true);
                _leftHandIKPos = _currentWeapon.GetLeftHandTransform();
                _rightHandIKPos = _currentWeapon.GetRightHandTransform();
                _leftHandTarget.position = _leftHandIKPos.position;
                _leftHandTarget.rotation = _leftHandIKPos.rotation;

                _rightHandTarget.position = _rightHandIKPos.position;
                _rightHandTarget.rotation = _rightHandIKPos.rotation;
            }
            else
            {
                ApplyWeaponPose(_equipPos, false);
                
            }


        }

        private void ApplyWeaponPose(Transform socket, bool aiming)
        {
            if (_currentWeaponTransform == null || socket == null) return;

            if (_currentWeaponTransform.parent != socket)
            {
                _currentWeaponTransform.SetParent(socket, true);
            }

            float transition = _aimTransitionDuration > 0f
                ? Time.deltaTime / _aimTransitionDuration
                : 1f;
            _currentWeaponTransform.localPosition = Vector3.Lerp(
                _currentWeaponTransform.localPosition, Vector3.zero, transition);
            _currentWeaponTransform.localRotation = Quaternion.Slerp(
                _currentWeaponTransform.localRotation, Quaternion.identity, transition);

            float ikWeight = Mathf.MoveTowards(
                _leftHandIKConstraint.weight,
                aiming ? 1f : 0f,
                transition);
            _leftHandIKConstraint.weight = ikWeight;
            _rightHandIKConstraint.weight = ikWeight;
        }

        private void ApplyWeaponSocketPoses()
        {
            if (CurrentWeapon == null) return;

            if (_equipPos != null)
            {
                _equipPos.localPosition = CurrentWeapon.EquipLocalPosition;
                _equipPos.localRotation = CurrentWeapon.EquipLocalRotation;
            }

            if (_aimPos != null)
            {
                _aimPos.localPosition = CurrentWeapon.AimLocalPosition;
                _aimPos.localRotation = CurrentWeapon.AimLocalRotation;
            }
        }
    }
}
