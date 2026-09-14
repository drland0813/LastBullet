using UnityEngine;
using UnityEngine.UI;

namespace LastBullet
{
    public class GameplayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponController _weaponController;

        [Header("Weapon Slots")]
        [Tooltip("Big image on the fire button. Shows the active weapon icon.")]
        [SerializeField] private Image _activeWeaponImage;
        [Tooltip("Small button above the fire button. Shows the inactive weapon icon. Tap to swap.")]
        [SerializeField] private Button _swapSlotButton;
        [SerializeField] private Image _swapWeaponImage;


        private void Awake()
        {
            if (_weaponController == null)
            {
                _weaponController = FindFirstObjectByType<WeaponController>();
            }

            if (_swapSlotButton != null)
            {
                _swapSlotButton.onClick.AddListener(OnSwapButtonClicked);
            }
        }

        private void Start()
        {
            if (_weaponController != null)
            {
                _weaponController.OnLoadoutChanged += RefreshWeaponSlots;
                _weaponController.OnActiveSlotChanged += RefreshWeaponSlots;
            }

            RefreshWeaponSlots();
        }

        private void OnDestroy()
        {
            if (_swapSlotButton != null)
            {
                _swapSlotButton.onClick.RemoveListener(OnSwapButtonClicked);
            }

            if (_weaponController != null)
            {
                _weaponController.OnLoadoutChanged -= RefreshWeaponSlots;
                _weaponController.OnActiveSlotChanged -= RefreshWeaponSlots;
            }
        }

        private void OnSwapButtonClicked()
        {
            if (_weaponController == null) return;
            _weaponController.ToggleSlot();
        }

        private void RefreshWeaponSlots()
        {
            if (_weaponController == null)
            {
                SetSlotVisual(_activeWeaponImage, null);
                SetSlotVisual(_swapWeaponImage, null);
                SetSwapInteractable(false);
                return;
            }

            WeaponBase activeWeapon = _weaponController.CurrentWeapon;
            WeaponSlot otherSlot = _weaponController.ActiveSlot == WeaponSlot.Primary
                ? WeaponSlot.Secondary
                : WeaponSlot.Primary;
            WeaponBase otherWeapon = _weaponController.GetSlotWeapon(otherSlot);

            SetSlotVisual(_activeWeaponImage, activeWeapon);
            SetSlotVisual(_swapWeaponImage, otherWeapon);
            SetSwapInteractable(otherWeapon != null);
        }

        private void SetSlotVisual(Image image, WeaponBase weapon)
        {
            if (image == null) return;

            Sprite icon = weapon != null && weapon.Data != null ? weapon.Data.Icon : null;
            image.sprite = icon;
            image.enabled = icon != null;
        }

        private void SetSwapInteractable(bool interactable)
        {
            if (_swapSlotButton != null)
            {
                _swapSlotButton.interactable = interactable;
            }
        }
    }
}
