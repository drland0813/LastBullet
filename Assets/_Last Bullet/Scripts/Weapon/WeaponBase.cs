using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class WeaponBase : MonoBehaviour, IWeapon
    {
        [Header("Weapon Data")]
        [SerializeField] protected GunDataSO _data;

        [Header("Character Animation")]
        [SerializeField] private AnimatorOverrideController _animationOverride;

        [Header("Character Socket Pose")]
        [SerializeField] private Vector3 _equipLocalPosition;
        [SerializeField] private Vector3 _equipLocalEulerAngles;
        [SerializeField] private Vector3 _aimLocalPosition;
        [SerializeField] private Vector3 _aimLocalEulerAngles;

        [Header("Fire Point")]
        [SerializeField] private Transform _muzzlePoint;
        
        [SerializeField] private ParticleSystem _muzzleFlashVFX;

        [Header("Rigging")] 
        [SerializeField] private Transform _leftHandPos;
        [SerializeField] private Transform _rightHandPos;
        [SerializeField] private GunRecoilMotion  _recoilMotion;
        public WeaponDataBase Data => _data;
        public AnimatorOverrideController AnimationOverride => _animationOverride;
        public Vector3 EquipLocalPosition => _equipLocalPosition;
        public Quaternion EquipLocalRotation => Quaternion.Euler(_equipLocalEulerAngles);
        public Vector3 AimLocalPosition => _aimLocalPosition;
        public Quaternion AimLocalRotation => Quaternion.Euler(_aimLocalEulerAngles);
        public bool CanFire => _currentAmmo > 0 && !_isReloading && Time.time >= _nextFireTime;
        public bool IsReloading => _isReloading;
        public int CurrentAmmo => _currentAmmo;

        public Transform GetTransform()
        {
            return transform;
        }

        public Transform GetLeftHandTransform()
        {
            return _leftHandPos;
        }

        public Transform GetRightHandTransform()
        {
            return _rightHandPos;
        }

        public event Action<int, int> OnAmmoChanged;
        public event Action OnFirePerformed;
        public event Action OnReloadStarted;
        public event Action OnReloadFinished;
        public Transform GetFirePoint()
        {
            return _muzzlePoint;
        }

        private int _currentAmmo;
        private bool _isReloading;
        private float _nextFireTime;
        private bool _fireInputHeld;
        private Coroutine _reloadCoroutine;

        private AudioSource _audioSource;
        protected ObjectPool<BulletProjectile> _bulletPool;
        protected ObjectPool<MuzzleFlash> _muzzleFlashPool;
        protected virtual void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            if (_data != null)
            {
                _currentAmmo = _data.MagazineSize;
            }
        }

        private void Start()
        {
            if (_data == null)
            {
                Debug.LogError($"[WeaponBase] {name}: No GunDataSO assigned to _data!", this);
                return;
            }

            // Only projectile weapons (e.g. grenade launcher) need a bullet pool.
            if (_data.BulletPrefab != null)
            {
                _bulletPool = BulletObjectPoolManager.Instance.GetBulletPool(_data.BulletPrefab);
            }
        }

        public void Fire(Vector3 origin, Vector3 direction)
        {
            if (!CanFire)
            {
                if (_currentAmmo <= 0 && !_isReloading)
                {
                    PlaySound(_data.EmptySound);
                    Reload();
                }
                return;
            }

            if (_data.FireMode == FireMode.SemiAuto && _fireInputHeld) return;
            _fireInputHeld = true;

            _currentAmmo--;
            _nextFireTime = Time.time + (1f / _data.FireRate);

            SpawnMuzzleFlash();
            PlaySound(_data.FireSound);
            ShootInternal(origin, direction);
            _recoilMotion.Play();
            OnFirePerformed?.Invoke();
            OnAmmoChanged?.Invoke(_currentAmmo, _data.MagazineSize);

            if (_currentAmmo <= 0)
            {
                Reload();
            }
        }

        public void ReleaseFire()
        {
            _fireInputHeld = false;
        }

        public void Reload()
        {
            if (_isReloading || _currentAmmo == _data.MagazineSize) return;

            if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = StartCoroutine(ReloadCoroutine());
        }

        public virtual void OnEquip()
        {
            gameObject.SetActive(true);
        }

        public virtual void OnUnequip()
        {
            if (_reloadCoroutine != null)
            {
                StopCoroutine(_reloadCoroutine);
                _isReloading = false;
            }
            gameObject.SetActive(false);
        }

        protected abstract void ShootInternal(Vector3 origin, Vector3 direction);

        private IEnumerator ReloadCoroutine()
        {
            _isReloading = true;
            OnReloadStarted?.Invoke();
            PlaySound(_data.ReloadSound);

            yield return new WaitForSeconds(_data.ReloadTime);

            _currentAmmo = _data.MagazineSize;
            _isReloading = false;

            OnReloadFinished?.Invoke();
            OnAmmoChanged?.Invoke(_currentAmmo, _data.MagazineSize);
        }

        private void SpawnMuzzleFlash()
        {
            if (_data.MuzzleFlashPrefab == null || _muzzlePoint == null) return;

            _muzzleFlashPool = BulletObjectPoolManager.Instance.GetMuzzleFlashPool();
            var flash = _muzzleFlashPool.Get();
            flash.SetPool(_muzzleFlashPool);
            flash.transform.position = GetMuzzlePosition();
            flash.transform.SetParent(_muzzlePoint.transform, true);
            // GameObject flash = Instantiate(_data.MuzzleFlashPrefab, _muzzlePoint);
            // Destroy(flash, 0.1f);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || _audioSource == null) return;
            _audioSource.PlayOneShot(clip);
        }

        protected Vector3 GetMuzzlePosition()
        {
            return _muzzlePoint != null ? _muzzlePoint.position : transform.position;
        }

        private void EnableMuzzleFlash()
        {
            if (_muzzleFlashVFX == null) return;
            _muzzleFlashVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _muzzleFlashVFX.Play(true);
        }
    }
}