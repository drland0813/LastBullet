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
        private static readonly int ParamWeaponType  = Animator.StringToHash("WeaponType");
        private static readonly int ParamIsFiring    = Animator.StringToHash("IsFiring");

        [SerializeField] private WeaponController _weaponController;
        
        private bool _isArmed = false;
        private bool _fireSequenceActive;
        private InputManager _inputManager;
        private void Start()
        {
            _weaponController.OnFireStarted += TriggerFire;
            _inputManager = InputManager.Instance;
        }


        void Update()
        {
            if (_isArmed && _weaponController.CurrentWeapon != null &&
                _weaponController.ForceFiring && !_fireSequenceActive)
            {
                TriggerFire();
            }
            
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

            StartCoroutine(FireSequence());
        }

        private IEnumerator FireSequence()
        {
            _fireSequenceActive = true;
            StartCoroutine(FadeLayerWeight(2, 1f, 0.05f));
            _animator.SetBool(ParamIsFiring, true);
            
            // _weaponController.CanFire = false;
            yield return new WaitUntil(() =>
                !InputManager.Instance.PlayerInputs.fire && !_weaponController.ForceFiring);
            _animator.SetBool(ParamIsFiring, false);
            
            // _weaponController.CanFire = true;
            //
            // float clipLength = _animator.GetCurrentAnimatorStateInfo(2).length;
            // yield return new WaitForSeconds(clipLength * 0.3f);
            //
            StartCoroutine(FadeLayerWeight(2, 0f, 0.1f));
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