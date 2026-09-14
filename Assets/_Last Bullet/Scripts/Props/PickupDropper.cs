using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    [Serializable]
    public class PickupDropEntry
    {
        public PickupItem Prefab;
        [Min(0f)] public float Weight = 1f;
    }

    public class PickupDropper : MonoBehaviour
    {
        [Header("Drop")]
        [SerializeField] private List<PickupDropEntry> _dropTable = new List<PickupDropEntry>();
        [SerializeField] [Range(0f, 1f)] private float _dropChance = 0.35f;
        [SerializeField] [Min(0f)] private float _spawnHeight = 0.5f;

        public void DropAt(Vector3 position)
        {
            if (_dropTable == null || _dropTable.Count == 0) return;
            if (UnityEngine.Random.value > _dropChance) return;

            PickupItem prefab = PickPrefab();
            if (prefab == null) return;

            Vector3 spawnPosition = position + Vector3.up * _spawnHeight;
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }

        private PickupItem PickPrefab()
        {
            float totalWeight = 0f;
            foreach (PickupDropEntry entry in _dropTable)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0f) continue;
                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (PickupDropEntry entry in _dropTable)
            {
                if (entry == null || entry.Prefab == null || entry.Weight <= 0f) continue;
                roll -= entry.Weight;
                if (roll <= 0f) return entry.Prefab;
            }

            return null;
        }
    }
}
