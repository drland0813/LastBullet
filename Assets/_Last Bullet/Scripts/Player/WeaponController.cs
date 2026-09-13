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
        [Header("Body Recoil")]
        [SerializeField] private PlayerBodyRecoil _bodyRecoil;
        private Transform _firePoint;
        public bool CanFire;

        [Header("Testing")]
        [SerializeField] private bool _forceFiring;
        [SerializeField] private float _firstShotDelay = 0.1f;
        public bool ForceFiring
        {
            get => _forceFiring;
            set => _forceFiring = value;
        }
        
        private PlayerInputs _input;
        private IWeapon _currentWeapon;
        private Transform _currentWeaponTransform;
        private bool _isFiring = false;
        private bool _isFiringMode;
        private bool _wasFirePressed;
        private bool _weaponPoseDirty = true;
        private Coroutine _firstShotCoroutine;

        private void Awake()
        {
            if (_bodyRecoil == null)
            {
                _bodyRecoil = GetComponentInChildren<PlayerBodyRecoil>(true);
            }

            if (_bodyRecoil == null)
            {
                Debug.LogWarning("[WeaponController] PlayerBodyRecoil is not assigned and was not found in children.", this);
            }
        }
        
        public Action OnFirePerformed;
        public Action OnFireStarted;
        public Action OnFiringModeEnded;
        public WeaponBase CurrentWeapon => _currentWeapon as WeaponBase;
        public bool IsFiringMode => _isFiringMode;

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
            bool firePressed = _input.fire || ForceFiring;
            bool movementStarted = _input.move.sqrMagnitude > 0.0001f;

            if (movementStarted)
            {
                ExitFiringMode();
            }

            bool fireStarted = firePressed && !_wasFirePressed;
            if (fireStarted)
            {
                if (!_isFiringMode)
                {
                    EnterFiringMode();
                    StartFirstShotDelay();
                }
                else
                {
                    FireCurrentWeapon();
                }
            }

            if (firePressed && _isFiringMode && _firstShotCoroutine == null)
            {
                FireCurrentWeapon();
            }

            if (!firePressed)
            {
                _currentWeapon.ReleaseFire();
            }

            _wasFirePressed = firePressed;
            _isFiring = _isFiringMode;

            if (_weaponPoseDirty)
            {
                UpdateWeaponTransform();
                _weaponPoseDirty = false;
            }

            if (_isFiringMode)
            {
                UpdateHandIKTargets();
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
            if (CurrentWeapon == newWeapon) return;

            ExitFiringMode();
            _wasFirePressed = false;

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
            _weaponPoseDirty = true;
            UpdateWeaponTransform();
            _weaponPoseDirty = false;
        }

        public void UnequipCurrentWeapon()
        {
            if (_currentWeapon == null) return;

            _currentWeapon.OnAmmoChanged -= HandleAmmoChanged;
            _currentWeapon.OnFirePerformed -= HandleWeaponFirePerformed;
            _currentWeapon.OnReloadStarted -= HandleReloadStarted;
            _currentWeapon.OnReloadFinished -= HandleReloadFinished;

            _currentWeapon.OnUnequip();
            _bodyRecoil?.ResetMotion();
            ExitFiringMode();
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
            if (CurrentWeapon != null)
            {
                _bodyRecoil?.Play(CurrentWeapon.BodyRecoil);
            }

            OnFirePerformed?.Invoke();
        }

        private void EnterFiringMode()
        {
            _isFiringMode = true;
            _weaponPoseDirty = true;
            OnFireStarted?.Invoke();
        }

        private void ExitFiringMode()
        {
            if (!_isFiringMode) return;

            if (_firstShotCoroutine != null)
            {
                StopCoroutine(_firstShotCoroutine);
                _firstShotCoroutine = null;
            }

            _isFiringMode = false;
            _weaponPoseDirty = true;
            _currentWeapon?.ReleaseFire();
            OnFiringModeEnded?.Invoke();
        }

        private void StartFirstShotDelay()
        {
            if (_firstShotCoroutine != null) return;
            _firstShotCoroutine = StartCoroutine(FirstShotCoroutine());
        }

        private System.Collections.IEnumerator FirstShotCoroutine()
        {
            yield return new WaitForSeconds(_firstShotDelay);
            _firstShotCoroutine = null;

            if (_isFiringMode)
            {
                FireCurrentWeapon();
            }
        }

        private void FireCurrentWeapon()
        {
            if (!_isFiringMode || _currentWeapon == null) return;

            Vector3 aimDirection = _aimController != null
                ? _aimController.GetAimDirection()
                : _firePoint.forward;
            Vector3 origin = _firePoint != null ? _firePoint.position : transform.position;
            _currentWeapon.Fire(origin, aimDirection);
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

            if (_isFiringMode)
            {
                ApplyWeaponPose(_aimPos, true);
            }
            else
            {
                ApplyWeaponPose(_equipPos, false);
                
            }


        }

        private void UpdateHandIKTargets()
        {
            if (_currentWeapon == null) return;

            _leftHandIKPos = _currentWeapon.GetLeftHandTransform();
            _rightHandIKPos = _currentWeapon.GetRightHandTransform();

            if (_leftHandIKPos != null && _leftHandTarget != null)
            {
                _leftHandTarget.position = _leftHandIKPos.position;
                _leftHandTarget.rotation = _leftHandIKPos.rotation;
            }

            if (_rightHandIKPos != null && _rightHandTarget != null)
            {
                _rightHandTarget.position = _rightHandIKPos.position;
                _rightHandTarget.rotation = _rightHandIKPos.rotation;
            }
        }

        private void ApplyWeaponPose(Transform socket, bool aiming)
        {
            if (_currentWeaponTransform == null || socket == null) return;

            if (_currentWeaponTransform.parent != socket)
            {
                _currentWeaponTransform.SetParent(socket, false);
                _currentWeaponTransform.localPosition = Vector3.zero;
                _currentWeaponTransform.localRotation = Quaternion.identity;
            }

            if (aiming)
            {
                _currentWeaponTransform.localPosition = Vector3.zero;
                _currentWeaponTransform.localRotation = Quaternion.identity;
                _leftHandIKConstraint.weight = 1f;
                _rightHandIKConstraint.weight = 1f;
                return;
            }

            _currentWeaponTransform.localPosition = Vector3.zero;
            _currentWeaponTransform.localRotation = Quaternion.identity;
            _leftHandIKConstraint.weight = 0f;
            _rightHandIKConstraint.weight = 0f;
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
