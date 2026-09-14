using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LastBullet
{
    public enum WeaponSlot
    {
        Primary,
        Secondary
    }

    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private AimController _aimController;

        [Header("Database")]
        [SerializeField] private WeaponDatabaseSO _weaponDatabase;

        [Header("Starting Loadout")]
        [SerializeField] private WeaponBase _startingWeapon;
        [SerializeField] private string _startingWeaponId;
        [SerializeField] private string _startingSecondaryId = "";

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
        private IWeapon _primaryWeapon;
        private IWeapon _secondaryWeapon;
        private WeaponSlot _activeSlot = WeaponSlot.Primary;
        private Transform _currentWeaponTransform;
        private bool _isFiring = false;
        private bool _isFiringMode;
        private bool _wasFirePressed;
        private bool _moveWasIdle = true;
        private bool _weaponPoseDirty = true;
        private Coroutine _firstShotCoroutine;

        [Header("Move Detection")]
        [Tooltip("Joystick magnitude below this is treated as rest. Must match the move deadzone on the movement controller.")]
        [SerializeField] [Min(0f)] private float _moveThreshold = 0.15f;

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
        public Action OnLoadoutChanged;
        public Action OnActiveSlotChanged;
        public WeaponBase CurrentWeapon => _currentWeapon as WeaponBase;
        public WeaponSlot ActiveSlot => _activeSlot;
        public bool IsFiringMode => _isFiringMode;
        public bool HasWeapon => _primaryWeapon != null || _secondaryWeapon != null;

        public WeaponBase GetSlotWeapon(WeaponSlot slot)
        {
            return GetSlot(slot) as WeaponBase;
        }

        public bool RefillCurrentWeapon()
        {
            if (CurrentWeapon == null || CurrentWeapon.Data == null) return false;
            if (CurrentWeapon.CurrentAmmo >= CurrentWeapon.Data.TotalAmmo) return false;

            CurrentWeapon.RefillAmmo();
            return true;
        }

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

        private void LateUpdate()
        {
            if (_currentWeapon == null || _aimController == null || _input == null) return;
            if (!(_input.fire || ForceFiring)) return;

            _aimController.RotateTowardsCurrentTarget();
        }

        private void HandleFire()
        {
            bool firePressed = _input.fire || ForceFiring;
            bool movementActive = _input.move.sqrMagnitude > _moveThreshold * _moveThreshold;

            if (firePressed && !movementActive)
            {
                _aimController?.SnapTowardsCurrentTarget();
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
            else if (_isFiringMode && ShouldExitFiringMode(firePressed, movementActive))
            {
                ExitFiringMode();
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
            _moveWasIdle = !movementActive;
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

        private bool ShouldExitFiringMode(bool firePressed, bool movementActive)
        {
            if (!movementActive) return false;

            bool isNewMoveGesture = _moveWasIdle;
            bool isReleasedWhileMoving = !firePressed && _wasFirePressed;
            return isNewMoveGesture || isReleasedWhileMoving;
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
                EquipWeaponByIdToSlot(_startingWeaponId, WeaponSlot.Primary);
            }
            else
            {
                EquipWeapon(_startingWeapon);
            }

            if (!string.IsNullOrEmpty(_startingSecondaryId))
            {
                EquipWeaponByIdToSlot(_startingSecondaryId, WeaponSlot.Secondary);
            }
        }

        public bool EquipWeaponById(string weaponId)
        {
            if (_weaponDatabase == null)
            {
                Debug.LogError("[WeaponController] WeaponDatabaseSO is missing.");
                return false;
            }

            WeaponBase weaponPrefab = _weaponDatabase.GetPrefabById(weaponId);
            if (weaponPrefab == null)
            {
                Debug.LogError($"[WeaponController] No weapon data found for id: {weaponId}");
                return false;
            }

            WeaponBase weapon = Instantiate(weaponPrefab, transform);
            if (weapon == null)
            {
                Debug.LogError($"[WeaponController] Prefab '{weaponPrefab.name}' does not contain WeaponBase.");
                Destroy(weapon.gameObject);
                return false;
            }

            WeaponSlot targetSlot = FindSlotForNewWeapon();
            EquipToSlot(weapon, targetSlot);
            if (targetSlot != _activeSlot)
            {
                SwitchToSlot(targetSlot);
            }
            return true;
        }

        private WeaponSlot FindSlotForNewWeapon()
        {
            if (GetSlot(WeaponSlot.Primary) == null) return WeaponSlot.Primary;
            if (GetSlot(WeaponSlot.Secondary) == null) return WeaponSlot.Secondary;
            return _activeSlot;
        }

        public bool EquipWeaponByIdToSlot(string weaponId, WeaponSlot slot)
        {
            if (_weaponDatabase == null)
            {
                Debug.LogError("[WeaponController] WeaponDatabaseSO is missing.");
                return false;
            }

            WeaponBase weaponPrefab = _weaponDatabase.GetPrefabById(weaponId);
            if (weaponPrefab == null)
            {
                Debug.LogError($"[WeaponController] No weapon data found for id: {weaponId}");
                return false;
            }

            WeaponBase weapon = Instantiate(weaponPrefab, transform);
            if (weapon == null)
            {
                Debug.LogError($"[WeaponController] Prefab '{weaponPrefab.name}' does not contain WeaponBase.");
                Destroy(weapon.gameObject);
                return false;
            }

            EquipToSlot(weapon, slot);
            return true;
        }

        public void EquipWeapon(WeaponBase newWeapon)
        {
            if (CurrentWeapon == newWeapon) return;

            ExitFiringMode();
            _wasFirePressed = false;

            ReplaceActiveSlot(newWeapon);
            OnLoadoutChanged?.Invoke();
            OnActiveSlotChanged?.Invoke();
        }

        public void SwitchToSlot(WeaponSlot slot)
        {
            if (slot == _activeSlot || GetSlot(slot) == null) return;

            ExitFiringMode();
            _wasFirePressed = false;

            StoreActiveWeapon();
            _activeSlot = slot;
            ActivateWeapon(GetSlot(slot));
            OnActiveSlotChanged?.Invoke();
        }

        public void ToggleSlot()
        {
            SwitchToSlot(_activeSlot == WeaponSlot.Primary ? WeaponSlot.Secondary : WeaponSlot.Primary);
        }

        private IWeapon GetSlot(WeaponSlot slot)
        {
            return slot == WeaponSlot.Primary ? _primaryWeapon : _secondaryWeapon;
        }

        private void SetSlot(WeaponSlot slot, IWeapon weapon)
        {
            if (slot == WeaponSlot.Primary)
            {
                _primaryWeapon = weapon;
            }
            else
            {
                _secondaryWeapon = weapon;
            }
        }

        private void EquipToSlot(WeaponBase newWeapon, WeaponSlot slot)
        {
            if (GetSlot(slot) as WeaponBase == newWeapon)
            {
                if (slot != _activeSlot)
                {
                    SwitchToSlot(slot);
                }
                return;
            }

            ExitFiringMode();
            _wasFirePressed = false;

            if (slot == _activeSlot)
            {
                ReplaceActiveSlot(newWeapon);
            }
            else
            {
                IWeapon oldWeapon = GetSlot(slot);
                if (oldWeapon != null)
                {
                    oldWeapon.OnUnequip();
                    Destroy(oldWeapon.GetTransform().gameObject);
                }

                SetSlot(slot, newWeapon);
                newWeapon.OnUnequip();
            }

            OnLoadoutChanged?.Invoke();
            OnActiveSlotChanged?.Invoke();
        }

        private void ReplaceActiveSlot(WeaponBase newWeapon)
        {
            IWeapon oldWeapon = GetSlot(_activeSlot);
            if (oldWeapon != null)
            {
                UnsubscribeWeapon(oldWeapon);
                oldWeapon.OnUnequip();
                Destroy(oldWeapon.GetTransform().gameObject);
            }

            SetSlot(_activeSlot, newWeapon);
            ActivateWeapon(newWeapon);
        }

        private void StoreActiveWeapon()
        {
            if (_currentWeapon == null) return;

            UnsubscribeWeapon(_currentWeapon);
            _currentWeapon.OnUnequip();
            _currentWeapon = null;
            _currentWeaponTransform = null;
            _leftHandIKPos = null;
            _rightHandIKPos = null;
        }

        private void ActivateWeapon(IWeapon weapon)
        {
            _currentWeapon = weapon;
            _currentWeapon.OnEquip();
            _leftHandIKPos = _currentWeapon.GetLeftHandTransform();
            _rightHandIKPos = _currentWeapon.GetRightHandTransform();
            _firePoint = _currentWeapon.GetFirePoint();
            SubscribeWeapon(_currentWeapon);
            _currentWeaponTransform = _currentWeapon.GetTransform();

            ApplyWeaponSocketPoses();
            _weaponPoseDirty = true;
            UpdateWeaponTransform();
            _weaponPoseDirty = false;
        }

        private void SubscribeWeapon(IWeapon weapon)
        {
            WeaponBase weaponBase = weapon as WeaponBase;
            if (weaponBase == null) return;

            weaponBase.OnAmmoChanged += HandleAmmoChanged;
            weaponBase.OnFirePerformed += HandleWeaponFirePerformed;
            weaponBase.OnReloadStarted += HandleReloadStarted;
            weaponBase.OnReloadFinished += HandleReloadFinished;
        }

        private void UnsubscribeWeapon(IWeapon weapon)
        {
            WeaponBase weaponBase = weapon as WeaponBase;
            if (weaponBase == null) return;

            weaponBase.OnAmmoChanged -= HandleAmmoChanged;
            weaponBase.OnFirePerformed -= HandleWeaponFirePerformed;
            weaponBase.OnReloadStarted -= HandleReloadStarted;
            weaponBase.OnReloadFinished -= HandleReloadFinished;
        }

        public void UnequipCurrentWeapon()
        {
            if (_currentWeapon == null) return;

            UnsubscribeWeapon(_currentWeapon);

            _currentWeapon.OnUnequip();
            _bodyRecoil?.ResetMotion();
            ExitFiringMode();
            Destroy(_currentWeaponTransform.gameObject);
            SetSlot(_activeSlot, null);
            _currentWeapon = null;
            _currentWeaponTransform = null;
            _leftHandIKPos = null;
            _rightHandIKPos = null;
            OnLoadoutChanged?.Invoke();

            WeaponSlot otherSlot = _activeSlot == WeaponSlot.Primary ? WeaponSlot.Secondary : WeaponSlot.Primary;
            if (GetSlot(otherSlot) != null)
            {
                SwitchToSlot(otherSlot);
            }
        }

        private void HandleAmmoChanged(int current, int max)
        {
            Debug.Log($"[WeaponController] Ammo: {current}/{max}");

            if (current <= 0)
            {
                AutoSwitchToLoadedSlot();
            }
        }

        private void AutoSwitchToLoadedSlot()
        {
            WeaponSlot otherSlot = _activeSlot == WeaponSlot.Primary ? WeaponSlot.Secondary : WeaponSlot.Primary;
            WeaponBase otherWeapon = GetSlot(otherSlot) as WeaponBase;
            if (otherWeapon != null && otherWeapon.CurrentAmmo > 0)
            {
                SwitchToSlot(otherSlot);
            }
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
