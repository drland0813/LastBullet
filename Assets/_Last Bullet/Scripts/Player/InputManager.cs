using UnityEngine;

namespace LastBullet
{
    public class InputManager : Singleton<InputManager>
    {
        [SerializeField] private PlayerInputs _playerInputs;

        public PlayerInputs PlayerInputs => _playerInputs;

        protected override void Awake()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
            base.Awake();

            if (_playerInputs == null)
            {
                _playerInputs = GetComponent<PlayerInputs>();
            }
        }
    }
}
