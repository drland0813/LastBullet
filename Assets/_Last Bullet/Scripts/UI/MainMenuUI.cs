using UnityEngine;
using UnityEngine.UI;

namespace LastBullet
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button _playButton;

        private void Awake()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void Start()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayMusic(SoundId.MainMenuMusic);
            }
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        private void OnPlayClicked()
        {
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.PlayGame();
            }
            else
            {
                Debug.LogError("[MainMenuUI] GameFlow is missing. Add a GameFlow object to the MainMenu scene.", this);
            }
        }
    }
}
