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
        private static readonly int ParamWeaponType  = Animator.StringToHash("WeaponType");
        private static readonly int ParamIsFiring    = Animator.StringToHash("IsFiring");

        [SerializeField] private WeaponController _weaponController;
        
        private bool _isArmed = false;
        private bool _fireSequenceActive;
        private Coroutine _fireSequenceCoroutine;
        private InputManager _inputManager;
        private void Start()
        {
            _weaponController.OnFireStarted += TriggerFire;
            _weaponController.OnFiringModeEnded += StopFireAnimation;
            _inputManager = InputManager.Instance;
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

        public void UnequipWeapon()
        {
            _isArmed = false;
            StopFireAnimation();
            _animator.runtimeAnimatorController = _baseController;
    
            _weaponController.UnequipCurrentWeapon();
            // Reset IK target
            // _leftHandTarget.SetParent(this.transform);
    
            StartCoroutine(FadeLayerWeight(1, 0f, 0.25f));
            // StartCoroutine(FadeRigWeight(0f, 0.25f));
            // _animator.SetInteger(ParamWeaponType, 0);
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