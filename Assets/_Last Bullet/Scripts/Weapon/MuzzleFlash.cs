using System;
using UnityEngine;

namespace LastBullet
{
    public class MuzzleFlash : MonoBehaviour
    {
        private  ObjectPool<MuzzleFlash> _muzzleFlashPool;
        private bool _isStored;

        private void OnEnable()
        {
            _isStored = false;
        }

        private void OnDisable()
        {
            if (_isStored) return;
            _isStored = true;

            // Already inactive here: store without touching active state
            // to avoid recursive SetActive calls.
            if (_muzzleFlashPool != null)
                _muzzleFlashPool.Store(this, false);
        }

        public void SetPool(ObjectPool<MuzzleFlash> muzzleFlashPool)
        {
            _muzzleFlashPool = muzzleFlashPool;
        }
        
    }
}