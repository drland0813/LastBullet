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
        [SerializeField] private AnimatorOverrideController _rifleOverride;
        [SerializeField] private RuntimeAnimatorController _baseController;
        
        [Header("IK")]
        [SerializeField] private Transform _leftHandTarget;
        [SerializeField] private Rig _weaponRig;
        private static readonly int ParamSpeed       = Animator.StringToHash("Speed");
        private static readonly int ParamWeaponType  = Animator.StringToHash("WeaponType");
        private static readonly int ParamIsFiring    = Animator.StringToHash("IsFiring");
        private static readonly int ParamIsReloading = Animator.StringToHash("IsReloading");

        private bool _isArmed = false;


        void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
                EquipWeapon(1, _leftHandTarget); 

            if (Input.GetKeyDown(KeyCode.Q))
                UnequipWeapon();

            if (Input.GetKeyDown(KeyCode.F) && _isArmed)
                TriggerFire();
        }

        public void EquipWeapon(int weaponType, Transform leftGripPoint)
        {
            _isArmed = true;
            _animator.SetInteger(ParamWeaponType, weaponType);
    
            _leftHandTarget.position = leftGripPoint.position;
            _leftHandTarget.rotation = leftGripPoint.rotation;
            _leftHandTarget.SetParent(leftGripPoint); // theo súng khi di chuyển

            if (weaponType == 1)
                _animator.runtimeAnimatorController = _rifleOverride;

            StartCoroutine(FadeLayerWeight(1, 1f, 0.25f));
            StartCoroutine(FadeRigWeight(1f, 0.25f));
        }

        public void UnequipWeapon()
        {
            _isArmed = false;
            _animator.runtimeAnimatorController = _baseController;
    
            // Reset IK target
            _leftHandTarget.SetParent(this.transform);
    
            StartCoroutine(FadeLayerWeight(1, 0f, 0.25f));
            StartCoroutine(FadeRigWeight(0f, 0.25f));
            _animator.SetInteger(ParamWeaponType, 0);
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
        
        public void SetSpeed(float speed)
        {
            _animator.SetFloat(ParamSpeed, speed);
        }

        public void TriggerFire()
        {
            StartCoroutine(FireSequence());
        }

        // ── Internal ────────────────────────────────────────────────
        private IEnumerator FireSequence()
        {
            StartCoroutine(FadeLayerWeight(2, 1f, 0.05f));
    
            _animator.SetBool(ParamIsFiring, true);
            yield return new WaitForSeconds(0.1f);
            _animator.SetBool(ParamIsFiring, false);

            float clipLength = _animator.GetCurrentAnimatorStateInfo(2).length;
            yield return new WaitForSeconds(clipLength * 0.8f);
    
            StartCoroutine(FadeLayerWeight(2, 0f, 0.1f));
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