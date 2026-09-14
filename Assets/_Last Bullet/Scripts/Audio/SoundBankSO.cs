using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    public enum SoundCategory
    {
        Music,
        Sfx
    }

    [Serializable]
    public class SoundEntry
    {
        public SoundId Id;
        public AudioClip Clip;
        public SoundCategory Category = SoundCategory.Sfx;
        [Range(0f, 1f)] public float Volume = 1f;
        [Min(0f)] public float PitchVariance = 0.04f;
        [Range(0f, 1f)] public float VolumeVariance = 0.05f;
        [Range(0f, 1f)] public float SpatialBlend = 1f;
    }

    [CreateAssetMenu(fileName = "SoundBank", menuName = "Last Bullet/Audio/Sound Bank")]
    public class SoundBankSO : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> _entries = new List<SoundEntry>();

        private Dictionary<SoundId, SoundEntry> _entryById;

        public bool TryGet(SoundId id, out SoundEntry entry)
        {
            if (_entryById == null)
            {
                _entryById = new Dictionary<SoundId, SoundEntry>();
                foreach (SoundEntry current in _entries)
                {
                    if (current == null || current.Clip == null) continue;
                    if (!_entryById.ContainsKey(current.Id))
                    {
                        _entryById.Add(current.Id, current);
                    }
                }
            }

            return _entryById.TryGetValue(id, out entry);
        }
    }
}
