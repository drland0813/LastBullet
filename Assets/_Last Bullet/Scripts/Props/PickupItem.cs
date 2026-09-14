using UnityEngine;

namespace LastBullet
{
    public enum PickupType
    {
        Weapon,
        Health,
        Ammo
    }

    [RequireComponent(typeof(Collider))]
    public class PickupItem : MonoBehaviour
    {
        [Header("Pickup")]
        [SerializeField] private PickupType _pickupType = PickupType.Health;
        [SerializeField] private string _weaponId = "G001";
        [SerializeField] private int _weaponType = 1;
        [SerializeField] [Min(0f)] private float _healAmount = 25f;
        [SerializeField] [Min(0f)] private float _lifetime = 20f;

        [Header("Visual")]
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private float _spinSpeed = 120f;
        [SerializeField] [Min(0f)] private float _bobHeight = 0.15f;
        [SerializeField] [Min(0f)] private float _bobSpeed = 2f;

        private float _age;
        private float _bobPhase;
        private Vector3 _visualBaseLocalPosition;

        public PickupType Type => _pickupType;

        private void Awake()
        {
            Collider trigger = GetComponent<Collider>();
            trigger.isTrigger = true;

            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }
            _visualBaseLocalPosition = _visualRoot.localPosition;
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_lifetime > 0f && _age >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            _visualRoot.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
            _visualRoot.localPosition = _visualBaseLocalPosition
                + Vector3.up * Mathf.Sin((Time.time + _bobPhase) * _bobSpeed) * _bobHeight;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;

            TopDownThirdPersonController player = other.GetComponentInParent<TopDownThirdPersonController>();
            if (player == null) return;

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health == null || health.IsDead) return;

            if (TryApply(player, health))
            {
                PlayPickupSound();
                Destroy(gameObject);
            }
        }

        private bool TryApply(TopDownThirdPersonController player, PlayerHealth health)
        {
            switch (_pickupType)
            {
                case PickupType.Weapon:
                    return ApplyWeapon(player);
                case PickupType.Health:
                    return ApplyHealth(health);
                case PickupType.Ammo:
                    return ApplyAmmo(player);
                default:
                    return false;
            }
        }

        private bool ApplyWeapon(TopDownThirdPersonController player)
        {
            if (string.IsNullOrEmpty(_weaponId)) return false;

            CharacterAnimationController animationController = player.GetComponent<CharacterAnimationController>();
            if (animationController != null)
            {
                return animationController.EquipWeaponById(_weaponId, _weaponType);
            }

            WeaponController weaponController = player.GetComponentInChildren<WeaponController>(true);
            if (weaponController == null) return false;

            return weaponController.EquipWeaponById(_weaponId);
        }

        private bool ApplyHealth(PlayerHealth health)
        {
            if (health.CurrentHealth >= health.MaxHealth) return false;

            health.RestoreHealth(_healAmount);
            return true;
        }

        private bool ApplyAmmo(TopDownThirdPersonController player)
        {
            WeaponController weaponController = player.GetComponentInChildren<WeaponController>(true);
            if (weaponController == null) return false;

            return weaponController.RefillCurrentWeapon();
        }

        private void PlayPickupSound()
        {
            if (SoundManager.Instance == null) return;

            switch (_pickupType)
            {
                case PickupType.Weapon:
                    SoundManager.Instance.PlaySfx(SoundId.PickupWeapon);
                    break;
                case PickupType.Health:
                    SoundManager.Instance.PlaySfx(SoundId.PickupHealth);
                    break;
                case PickupType.Ammo:
                    SoundManager.Instance.PlaySfx(SoundId.PickupAmmo);
                    break;
            }
        }
    }
}
