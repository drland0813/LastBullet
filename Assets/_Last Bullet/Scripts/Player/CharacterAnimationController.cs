using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LastBullet
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [Header("Override Controllers")]
        [SerializeField] private RuntimeAnimatorController _baseController;
        
        [Header("IK")]
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private Rig _weaponRig;
        [Header("Firing")]
        [SerializeField] private float _fireAnimationDuration = 0.25f;
        [Header("Hit Animation")]
        [SerializeField] private int _hitLayerIndex = 3;
        [SerializeField] [Min(0.1f)] private float _hitAnimationTimeout = 2f;
        private static readonly int ParamWeaponType  = Animator.StringToHash("WeaponType");
        private static readonly int ParamIsFiring    = Animator.StringToHash("IsFiring");
        private static readonly int ParamHit = Animator.StringToHash("Hit");
        private static readonly int StateHeadHit = Animator.StringToHash("Hit Layer.Head Hit");

        [SerializeField] private WeaponController _weaponController;
        [SerializeField] private bool _equipStartingWeaponOnStart = true;
        
        private bool _isArmed = false;
        private bool _fireSequenceActive;
        private Coroutine _fireSequenceCoroutine;
        private Coroutine _hitAnimationCoroutine;
        private InputManager _inputManager;

        private void Awake()
        {
        }

        private void Start()
        {
            SetHitLayerWeight(0f);
            _weaponController.OnFireStarted += TriggerFire;
            _weaponController.OnFiringModeEnded += StopFireAnimation;
            _weaponController.OnActiveSlotChanged += RefreshArmedVisuals;
            _inputManager = InputManager.Instance;

            if (_equipStartingWeaponOnStart && !_weaponController.HasWeapon)
            {
                EquipWeapon(1, null);
            }
        }

        private void OnDestroy()
        {
            if (_weaponController == null) return;

            _weaponController.OnFireStarted -= TriggerFire;
            _weaponController.OnFiringModeEnded -= StopFireAnimation;
            _weaponController.OnActiveSlotChanged -= RefreshArmedVisuals;
        }


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
                EquipWeapon(1, null); 

            if (Input.GetKeyDown(KeyCode.Q))
                UnequipWeapon();
            

        }

        public void EquipWeapon(int weaponType, Transform leftGripPoint)
        {
            _isArmed = true;
            _animator.SetInteger(ParamWeaponType, weaponType);

            _weaponController.EquipWeapon();
            ApplyWeaponAnimationOverride();

            StartCoroutine(FadeLayerWeight(1, 1f, 0.25f));
            // StartCoroutine(FadeRigWeight(1f, 0.25f));
        }

        public bool EquipWeaponById(string weaponId, int weaponType = 1)
        {
            if (_weaponController == null || string.IsNullOrEmpty(weaponId)) return false;
            if (!_weaponController.EquipWeaponById(weaponId)) return false;

            _isArmed = true;
            _animator.SetInteger(ParamWeaponType, weaponType);

            ApplyWeaponAnimationOverride();

            StartCoroutine(FadeLayerWeight(1, 1f, 0.25f));
            return true;
        }

        public void UnequipWeapon()
        {
            StopFireAnimation();
            _weaponController.UnequipCurrentWeapon();
            // Reset IK target
            // _leftHandTarget.SetParent(this.transform);

            RefreshArmedVisuals();
            // _animator.SetInteger(ParamWeaponType, 0);
        }

        private void RefreshArmedVisuals()
        {
            if (_weaponController != null && _weaponController.CurrentWeapon != null)
            {
                _isArmed = true;
                ApplyWeaponAnimationOverride();
                StartCoroutine(FadeLayerWeight(1, 1f, 0.1f));
            }
            else
            {
                _isArmed = false;
                _animator.runtimeAnimatorController = _baseController;
                StartCoroutine(FadeLayerWeight(1, 0f, 0.25f));
            }
        }

        private void ApplyWeaponAnimationOverride()
        {
            AnimatorOverrideController animationOverride = _weaponController.CurrentWeapon?.AnimationOverride;
            _animator.runtimeAnimatorController = animationOverride != null
                ? animationOverride
                : _baseController;
        }
        
        private IEnumerator FadeRigWeight(float target, float duration)
        {
            float start = _weaponRig.weight;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _weaponRig.weight = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            _weaponRig.weight = target;
        }
        

        public void TriggerFire()
        {
            if (_fireSequenceActive) return;

            _fireSequenceCoroutine = StartCoroutine(FireSequence());
        }

        public void PlayHitAnimation()
        {
            if (_animator == null) return;

            SetHitLayerWeight(1f);
            _animator.SetTrigger(ParamHit);

            if (_hitAnimationCoroutine != null)
            {
                StopCoroutine(_hitAnimationCoroutine);
            }

            _animator.Play(StateHeadHit, _hitLayerIndex, 0f);
            _hitAnimationCoroutine = StartCoroutine(DisableHitLayer());
        }

        private IEnumerator DisableHitLayer()
        {
            float timeout = Time.time + _hitAnimationTimeout;

            yield return null;

            while (Time.time < timeout)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(_hitLayerIndex);
                if (stateInfo.fullPathHash == StateHeadHit && stateInfo.normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }

            SetHitLayerWeight(0f);
            _hitAnimationCoroutine = null;
        }

        private void SetHitLayerWeight(float weight)
        {
            if (_animator.layerCount <= _hitLayerIndex) return;

            _animator.SetLayerWeight(_hitLayerIndex, weight);
        }

        private IEnumerator FireSequence()
        {
            _fireSequenceActive = true;
            StartCoroutine(FadeLayerWeight(2, 1f, 0.05f));
            _animator.SetBool(ParamIsFiring, true);

            yield return new WaitForSeconds(_fireAnimationDuration);

            while (_weaponController.IsFiringMode)
            {
                yield return null;
            }

            _animator.SetBool(ParamIsFiring, false);
            
            // _weaponController.CanFire = true;
            //
            // float clipLength = _animator.GetCurrentAnimatorStateInfo(2).length;
            // yield return new WaitForSeconds(clipLength * 0.3f);
            //
            StartCoroutine(FadeLayerWeight(2, 0f, 0.1f));
            _fireSequenceActive = false;
            _fireSequenceCoroutine = null;
        }

        private void StopFireAnimation()
        {
            if (_fireSequenceCoroutine != null)
            {
                StopCoroutine(_fireSequenceCoroutine);
                _fireSequenceCoroutine = null;
            }

            _animator.SetBool(ParamIsFiring, false);
            _animator.SetLayerWeight(2, 0f);
            _fireSequenceActive = false;
        }

        private IEnumerator FadeLayerWeight(int layer, float target, float duration)
        {
            float start   = _animator.GetLayerWeight(layer);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(start, target, elapsed / duration);
                _animator.SetLayerWeight(layer, weight);
                yield return null;
            }

            _animator.SetLayerWeight(layer, target);
        }
    }

}