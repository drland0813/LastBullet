using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBullet
{
    public class GameFlow : PersistentSingleton<GameFlow>
    {
        [Header("Scenes")]
        [SerializeField] private string _menuSceneName = "MainMenu";
        [SerializeField] private string _loadingSceneName = "Loading";
        [SerializeField] private string _gameplaySceneName = "Gameplay";

        private string _pendingTargetScene = "";
        private AsyncOperation _loadOperation;
        private bool _isLoading;

        public bool IsLoading => _isLoading;
        public float LoadProgress
        {
            get
            {
                if (_loadOperation == null) return 0f;
                return Mathf.Clamp01(_loadOperation.progress / 0.9f);
            }
        }

        protected override void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        public void PlayGame()
        {
            _pendingTargetScene = _gameplaySceneName;
            _isLoading = false;
            _loadOperation = null;
            Time.timeScale = 1f;
            SceneManager.LoadScene(_loadingSceneName);
        }

        public void ReloadGameplay()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_gameplaySceneName);
        }

        public void BackToMenu()
        {
            _pendingTargetScene = _menuSceneName;
            _isLoading = false;
            _loadOperation = null;
            Time.timeScale = 1f;
            SceneManager.LoadScene(_loadingSceneName);
        }

        public void BeginLoadTarget()
        {
            if (_isLoading) return;
            if (string.IsNullOrEmpty(_pendingTargetScene))
            {
                _pendingTargetScene = _gameplaySceneName;
            }

            StartCoroutine(LoadTargetSequence(_pendingTargetScene));
        }

        private IEnumerator LoadTargetSequence(string targetScene)
        {
            _isLoading = true;
            _loadOperation = SceneManager.LoadSceneAsync(targetScene);
            _loadOperation.allowSceneActivation = false;

            while (_loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            _loadOperation.allowSceneActivation = true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;

            if (_loadOperation != null && scene.name == _pendingTargetScene)
            {
                _loadOperation = null;
                _isLoading = false;
                _pendingTargetScene = "";
            }

            if (SoundManager.Instance == null) return;

            if (scene.name == _gameplaySceneName)
            {
                SoundManager.Instance.PlayMusic(SoundId.GameplayMusic);
            }
            else if (scene.name == _menuSceneName)
            {
                SoundManager.Instance.PlayMusic(SoundId.MainMenuMusic);
            }
        }
    }
}
