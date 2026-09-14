using System;
using UnityEngine;

namespace LastBullet
{
    public enum MatchState
    {
        Playing,
        Won,
        Lost
    }

    [Serializable]
    public class DifficultyPhase
    {
        [Range(0f, 1f)] public float StartProgress;
        [Min(0.1f)] public float SpawnInterval = 2f;
        [Min(1)] public int MaxAlive = 12;
        [Min(0.1f)] public float SpeedMultiplier = 1f;
        [Min(0.1f)] public float HealthMultiplier = 1f;
    }

    public class GameManager : Singleton<GameManager>
    {
        [Header("Match")]
        [SerializeField] [Min(10f)] private float _matchDuration = 180f;
        [SerializeField] [Min(0f)] private float _loseDelay = 3f;

        [Header("Difficulty")]
        [SerializeField] private DifficultyPhase[] _phases = new DifficultyPhase[]
        {
            new DifficultyPhase { StartProgress = 0f, SpawnInterval = 2.5f, MaxAlive = 8, SpeedMultiplier = 1f, HealthMultiplier = 1f },
            new DifficultyPhase { StartProgress = 0.33f, SpawnInterval = 1.2f, MaxAlive = 16, SpeedMultiplier = 1.25f, HealthMultiplier = 1.5f },
            new DifficultyPhase { StartProgress = 0.66f, SpawnInterval = 0.6f, MaxAlive = 24, SpeedMultiplier = 1.5f, HealthMultiplier = 2f }
        };

        [Header("References")]
        [SerializeField] private ZombieManager _zombieManager;
        [SerializeField] private PlayerHealth _playerHealth;

        private MatchState _state = MatchState.Playing;
        private float _timeLeft;
        private int _killCount;
        private int _appliedPhaseIndex = -1;
        private bool _isPlayerDead;

        public MatchState State => _state;
        public float TimeLeft => Mathf.Max(0f, _timeLeft);
        public float MatchDuration => _matchDuration;
        public float Progress => Mathf.Clamp01(1f - _timeLeft / _matchDuration);
        public int KillCount => _killCount;
        public bool IsPlaying => _state == MatchState.Playing;
        public event Action<bool> OnMatchEnded;

        private void Start()
        {
            if (_zombieManager == null)
            {
                _zombieManager = FindFirstObjectByType<ZombieManager>();
            }

            if (_playerHealth == null)
            {
                _playerHealth = FindFirstObjectByType<PlayerHealth>();
            }

            _timeLeft = _matchDuration;
            _appliedPhaseIndex = -1;
            ApplyDifficulty(0f);

            if (_zombieManager != null)
            {
                _zombieManager.OnZombieKilled += HandleZombieKilled;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnDied += HandlePlayerDied;
            }
        }

        private void OnDestroy()
        {
            if (_zombieManager != null)
            {
                _zombieManager.OnZombieKilled -= HandleZombieKilled;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnDied -= HandlePlayerDied;
            }
        }

        private void Update()
        {
            if (_state != MatchState.Playing || _isPlayerDead) return;

            _timeLeft -= Time.deltaTime;
            ApplyDifficulty(Progress);

            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                WinMatch();
            }
        }

        private void ApplyDifficulty(float progress)
        {
            if (_zombieManager == null || _phases == null || _phases.Length == 0) return;

            int phaseIndex = 0;
            for (int i = 0; i < _phases.Length; i++)
            {
                if (_phases[i] != null && progress >= _phases[i].StartProgress)
                {
                    phaseIndex = i;
                }
            }

            if (phaseIndex == _appliedPhaseIndex) return;
            _appliedPhaseIndex = phaseIndex;

            DifficultyPhase phase = _phases[phaseIndex];
            _zombieManager.SetDifficulty(phase.SpawnInterval, phase.MaxAlive, phase.SpeedMultiplier, phase.HealthMultiplier);
        }

        private void HandleZombieKilled(ZombieAI zombie)
        {
            if (_state != MatchState.Playing) return;
            _killCount++;
        }

        private void HandlePlayerDied()
        {
            if (_state != MatchState.Playing || _isPlayerDead) return;

            _isPlayerDead = true;
            _zombieManager?.StopSpawning();
            StartCoroutine(LoseSequence());
        }

        private System.Collections.IEnumerator LoseSequence()
        {
            yield return new WaitForSeconds(_loseDelay);

            if (_state == MatchState.Playing)
            {
                LoseMatch();
            }
        }

        private void WinMatch()
        {
            _state = MatchState.Won;
            _zombieManager?.StopSpawning();
            OnMatchEnded?.Invoke(true);
        }

        private void LoseMatch()
        {
            _state = MatchState.Lost;
            _zombieManager?.StopSpawning();
            OnMatchEnded?.Invoke(false);
        }
    }
}
