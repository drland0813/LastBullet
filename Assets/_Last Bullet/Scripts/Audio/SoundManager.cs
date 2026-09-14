using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    public class SoundManager : PersistentSingleton<SoundManager>
    {
        [Header("Bank")]
        [SerializeField] private SoundBankSO _soundBank;

        [Header("Music")]
        [SerializeField] private bool _autoplayOnStart = true;
        [SerializeField] private SoundId _autoplayMusicId = SoundId.GameplayMusic;
        [SerializeField] [Min(0f)] private float _autoplayFadeTime = 1.5f;

        [Header("Pool")]
        [SerializeField] [Min(1)] private int _sfxPoolSize = 12;
        [SerializeField] [Min(0f)] private float _defaultSpatialBlend = 1f;

        [Header("Volume")]
        [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume = 1f;

        private const string MusicVolumeKey = "LastBullet.MusicVolume";
        private const string SfxVolumeKey = "LastBullet.SfxVolume";

        private AudioSource _musicSource;
        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _sfxPoolIndex;
        private SoundId? _currentMusic;
        private float _musicBaseVolume = 1f;
        private Coroutine _musicFadeCoroutine;
        private HashSet<SoundId> _missingWarningShown = new HashSet<SoundId>();

        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;

        protected override void Awake()
        {
            if (Instance != null && Instance.gameObject != null)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake();

            _musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, _musicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, _sfxVolume);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;

            for (int i = 0; i < _sfxPoolSize; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.loop = false;
                source.playOnAwake = false;
                _sfxPool.Add(source);
            }

            ApplyVolumes();
        }

        private void Start()
        {
            if (_autoplayOnStart)
            {
                PlayMusic(_autoplayMusicId, _autoplayFadeTime);
            }
        }

        public void PlayMusic(SoundId id, float fadeTime = 1f)
        {
            if (_soundBank == null || !_soundBank.TryGet(id, out SoundEntry entry))
            {
                WarnMissing(id);
                return;
            }

            if (_currentMusic == id && _musicSource.isPlaying) return;
            _currentMusic = id;

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            _musicFadeCoroutine = StartCoroutine(FadeMusicTo(entry.Clip, entry.Volume, fadeTime));
        }

        public void StopMusic(float fadeTime = 0.5f)
        {
            _currentMusic = null;

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }
            _musicFadeCoroutine = StartCoroutine(FadeMusicTo(null, 0f, fadeTime));
        }

        public void PlaySfx(SoundId id)
        {
            if (_soundBank == null || !_soundBank.TryGet(id, out SoundEntry entry))
            {
                WarnMissing(id);
                return;
            }

            PlayOnPooledSource(entry, Vector3.zero, false);
        }

        public void PlaySfxAt(SoundId id, Vector3 position)
        {
            if (_soundBank == null || !_soundBank.TryGet(id, out SoundEntry entry))
            {
                WarnMissing(id);
                return;
            }

            PlayOnPooledSource(entry, position, true);
        }

        public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariance = 0f)
        {
            if (clip == null) return;
            if (_sfxPool.Count == 0) return;

            AudioSource source = NextSfxSource();
            source.transform.position = position;
            source.spatialBlend = _defaultSpatialBlend;
            source.volume = volume * _sfxVolume;
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.clip = clip;
            source.Play();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolume);
            PlayerPrefs.Save();
            ApplyVolumes();
        }

        private void PlayOnPooledSource(SoundEntry entry, Vector3 position, bool usePosition)
        {
            if (_sfxPool.Count == 0 || entry.Clip == null) return;

            AudioSource source = NextSfxSource();
            if (usePosition)
            {
                source.transform.position = position;
            }
            source.spatialBlend = entry.SpatialBlend;
            source.volume = entry.Volume * _sfxVolume * (1f - Random.Range(0f, entry.VolumeVariance));
            source.pitch = 1f + Random.Range(-entry.PitchVariance, entry.PitchVariance);
            source.clip = entry.Clip;
            source.Play();
        }

        private AudioSource NextSfxSource()
        {
            AudioSource source = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Count;
            return source;
        }

        private IEnumerator FadeMusicTo(AudioClip clip, float targetVolume, float fadeTime)
        {
            float startVolume = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime && fadeTime > 0f)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.clip = clip;
            _musicBaseVolume = targetVolume;

            if (clip == null)
            {
                _musicFadeCoroutine = null;
                yield break;
            }

            _musicSource.Play();

            elapsed = 0f;
            while (elapsed < fadeTime && fadeTime > 0f)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(0f, _musicBaseVolume * _musicVolume, elapsed / fadeTime);
                yield return null;
            }

            _musicSource.volume = _musicBaseVolume * _musicVolume;
            _musicFadeCoroutine = null;
        }

        private void ApplyVolumes()
        {
            if (_musicSource != null)
            {
                _musicSource.volume = _musicBaseVolume * _musicVolume;
            }
        }

        private void WarnMissing(SoundId id)
        {
            if (_missingWarningShown.Contains(id)) return;
            _missingWarningShown.Add(id);
            Debug.LogWarning($"[SoundManager] No clip assigned for '{id}'. Add it to the Sound Bank.", this);
        }
    }
}
