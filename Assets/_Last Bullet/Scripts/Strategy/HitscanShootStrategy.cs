using UnityEngine;
namespace LastBullet
{
    public class HitscanShootStrategy : IHitscanStrategy
    {
        public void Execute(Vector3 origin, Vector3 direction, GunDataSO data,
                            GameObject instigator)
        {
            Vector3 aimedDirection = ApplySpread(direction, data.Spread);
            FireRay(origin, aimedDirection, data, data.Damage, data.HitForce, instigator);
        }

        protected void FireRay(Vector3 origin, Vector3 direction, GunDataSO data,
                               float damage, float hitForce, GameObject instigator)
        {
            Vector3 endPoint = origin + direction * data.Range;
            Collider hitCollider = null;

            RaycastHit[] hits = Physics.RaycastAll(origin, direction, data.Range);
            hits = SortByDistance(hits);

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (IsPartOfInstigator(hit.collider, instigator)) continue;
                if (hit.collider.GetComponentInParent<PickupItem>() != null) continue;

                hitCollider = hit.collider;
                endPoint = hit.point;
                break;
            }

            if (hitCollider != null)
            {
                IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(new HitData
                    {
                        Amount = damage,
                        Instigator = instigator,
                        Point = endPoint,
                        Direction = direction,
                        Force = hitForce
                    });
                }

                SpawnHitImpact(ResolveImpactPrefab(hitCollider, data.HitImpactPrefab), endPoint);
            }

            SpawnTracer(data.TracerPrefab, origin, endPoint, data.TracerLifetime);
        }

        private GameObject ResolveImpactPrefab(Collider hitCollider, GameObject defaultPrefab)
        {
            ZombieAI zombie = hitCollider.GetComponentInParent<ZombieAI>();
            if (zombie != null && zombie.TryGetBloodImpact(out GameObject bloodPrefab))
            {
                return bloodPrefab;
            }

            return defaultPrefab;
        }

        protected Vector3 ApplySpread(Vector3 direction, float spread)
        {
            if (spread <= 0f) return direction;

            float halfSpread = spread * 0.5f;
            float randomYaw = Random.Range(-halfSpread, halfSpread);
            float randomPitch = Random.Range(-halfSpread, halfSpread);

            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);
            return spreadRotation * direction;
        }

        private bool IsPartOfInstigator(Collider collider, GameObject instigator)
        {
            if (instigator == null) return false;

            Transform root = instigator.transform;
            while (root.parent != null)
            {
                root = root.parent;
            }

            Transform t = collider.transform;
            while (t != null)
            {
                if (t == root) return true;
                t = t.parent;
            }
            return false;
        }

        private RaycastHit[] SortByDistance(RaycastHit[] hits)
        {
            for (int i = 1; i < hits.Length; i++)
            {
                RaycastHit key = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].distance > key.distance)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }
                hits[j + 1] = key;
            }
            return hits;
        }

        private void SpawnHitImpact(GameObject impactPrefab, Vector3 hitPoint)
        {
            if (impactPrefab == null) return;

            GameObject impact = Object.Instantiate(impactPrefab, hitPoint, Quaternion.identity);
            Object.Destroy(impact, 2f);
        }

        private void SpawnTracer(GameObject tracerPrefab, Vector3 from, Vector3 to,
                                 float lifetime)
        {
            if (tracerPrefab == null || lifetime <= 0f) return;

            BulletTracer tracerObject = GetPooledTracer(tracerPrefab);
            if (tracerObject == null) return;

            tracerObject.gameObject.SetActive(true);
            tracerObject.Show(from, to, lifetime);
        }

        private BulletTracer GetPooledTracer(GameObject tracerPrefab)
        {
            BulletTracer tracerPrefabComponent = tracerPrefab.GetComponent<BulletTracer>();
            if (tracerPrefabComponent == null) return null;

            ObjectPool<BulletTracer> bulletPool =
                BulletObjectPoolManager.Instance.GetBulletPool(tracerPrefabComponent);
            if (bulletPool == null) return null;

            var tracer = bulletPool.GetInactive();
            tracer.SetPool(bulletPool);
            return tracer;
        }

    }
}