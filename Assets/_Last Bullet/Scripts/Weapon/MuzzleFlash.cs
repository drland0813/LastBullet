using System;
using UnityEngine;

namespace LastBullet
{
    public class MuzzleFlash : MonoBehaviour
    {
        private  ObjectPool<MuzzleFlash> _muzzleFlashPool;

        private void OnDisable()
        {
            _muzzleFlashPool.Store(this);
        }

        public void SetPool(ObjectPool<MuzzleFlash> muzzleFlashPool)
        {
            _muzzleFlashPool = muzzleFlashPool;
        }
        
    }
}