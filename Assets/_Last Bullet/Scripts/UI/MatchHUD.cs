using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace LastBullet
{
    public class MatchHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager _gameManager;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _killText;
        [SerializeField] private Color _timerDangerColor = Color.red;
        [SerializeField] [Min(0f)] private float _dangerThreshold = 30f;
        [SerializeField] private float _dangerBlinkSpeed = 4f;

        [Header("Panels")]
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private TextMeshProUGUI _winStatsText;
        [SerializeField] private TextMeshProUGUI _loseStatsText;

        private Color _timerNormalColor;
        private bool _hasNormalColor;

        private void Awake()
        {
            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }

            if (_timerText != null)
            {
                _timerNormalColor = _timerText.color;
                _hasNormalColor = true;
            }
        }

        private void Start()
        {
            if (_gameManager != null)
            {
                _gameManager.OnMatchEnded += HandleMatchEnded;
            }

            SetPanelActive(_winPanel, false);
            SetPanelActive(_losePanel, false);
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.OnMatchEnded -= HandleMatchEnded;
            }
        }

        private void Update()
        {
            if (_gameManager == null) return;

            UpdateTimer();
            UpdateKills();
        }

        private void UpdateTimer()
        {
            if (_timerText == null) return;

            float timeLeft = _gameManager.TimeLeft;
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            _timerText.text = $"{minutes:00}:{seconds:00}";

            if (!_hasNormalColor) return;

            if (timeLeft <= _dangerThreshold && _gameManager.IsPlaying)
            {
                float blink = (Mathf.Sin(Time.unscaledTime * _dangerBlinkSpeed) + 1f) * 0.5f;
                _timerText.color = Color.Lerp(_timerNormalColor, _timerDangerColor, blink);
            }
            else
            {
                _timerText.color = _timerNormalColor;
            }
        }

        private void UpdateKills()
        {
            if (_killText == null) return;
            _killText.text = $"Kills: {_gameManager.KillCount}";
        }

        private void HandleMatchEnded(bool won)
        {
            if (won)
            {
                SetPanelActive(_winPanel, true);
                SetStatsText(_winStatsText, $"You survived!\nKills: {_gameManager.KillCount}");
            }
            else
            {
                SetPanelActive(_losePanel, true);
                float survived = _gameManager.MatchDuration - _gameManager.TimeLeft;
                int minutes = Mathf.FloorToInt(survived / 60f);
                int seconds = Mathf.FloorToInt(survived % 60f);
                SetStatsText(_loseStatsText, $"Kills: {_gameManager.KillCount}\nSurvived: {minutes:00}:{seconds:00}");
            }
        }

        private void SetStatsText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        public void ReplayMatch()
        {
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.ReloadGameplay();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void BackToMainMenu()
        {
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.BackToMenu();
                return;
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
