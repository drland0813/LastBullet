using System;
using System.Collections;
using UnityEngine;

namespace LastBullet
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class WeaponBase : MonoBehaviour, IWeapon
    {
        [Header("Weapon Data")]
        [SerializeField] protected GunDataSO _data;

        [Header("Fire Point")]
        [SerializeField] private Transform _muzzlePoint;

        public WeaponDataBase Data => _data;
        public bool CanFire => _currentAmmo > 0 && !_isReloading && Time.time >= _nextFireTime;
        public bool IsReloading => _isReloading;
        public int CurrentAmmo => _currentAmmo;

        public event Action<int, int> OnAmmoChanged;
        public event Action OnFirePerformed;
        public event Action OnReloadStarted;
        public event Action OnReloadFinished;

        private int _currentAmmo;
        private bool _isReloading;
        private float _nextFireTime;
        private bool _fireInputHeld;
        private Coroutine _reloadCoroutine;

        private AudioSource _audioSource;
        private ObjectPool<BulletProjectile> _bulletPool;
        private GameObject _poolRoot;

        protected virtual void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _currentAmmo = _data.MagazineSize;
            InitBulletPool();
        }

        private void InitBulletPool()
        {
            if (_data.BulletPrefab == null) return;

            _poolRoot = new GameObject($"[Pool] {_data.Name} Bullets");
            Debug.Log($"Init: [Pool] {_data.Name} Bullets");
            _poolRoot.transform.SetParent(transform);
            _poolRoot.transform.localPosition = Vector3.zero;

            BulletProjectile bulletProjectile = Instantiate(_data.BulletPrefab, _poolRoot.transform);
            bulletProjectile.gameObject.SetActive(false);

            _bulletPool = new ObjectPool<BulletProjectile>(bulletProjectile);
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
            ShootInternal(origin, direction, _bulletPool);

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

        protected abstract void ShootInternal(Vector3 origin, Vector3 direction,
                                              ObjectPool<BulletProjectile> pool);

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

            GameObject flash = Instantiate(_data.MuzzleFlashPrefab,
                                           _muzzlePoint.position,
                                           _muzzlePoint.rotation);
            Destroy(flash, 0.1f);
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
    }
}
