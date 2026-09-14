using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace LastBullet
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Fill-type Image used as the progress bar.")]
        [SerializeField] private Image _progressBar;
        [SerializeField] private TextMeshProUGUI _progressText;

        [Header("Fallback")]
        [Tooltip("Used only when testing the Loading scene directly without GameFlow.")]
        [SerializeField] private string _fallbackGameplayScene = "Gameplay";

        private AsyncOperation _fallbackOperation;

        private void Start()
        {
            if (GameFlow.Instance != null)
            {
                GameFlow.Instance.BeginLoadTarget();
            }
            else
            {
                Time.timeScale = 1f;
                _fallbackOperation = SceneManager.LoadSceneAsync(_fallbackGameplayScene);
            }

            RefreshVisual(0f);
        }

        private void Update()
        {
            float progress = GameFlow.Instance != null
                ? GameFlow.Instance.LoadProgress
                : FallbackProgress();

            RefreshVisual(progress);
        }

        private float FallbackProgress()
        {
            if (_fallbackOperation == null) return 1f;
            return Mathf.Clamp01(_fallbackOperation.progress / 0.9f);
        }

        private void RefreshVisual(float progress)
        {
            if (_progressBar != null)
            {
                _progressBar.fillAmount = progress;
            }

            if (_progressText != null)
            {
                _progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            }
        }
    }
}
