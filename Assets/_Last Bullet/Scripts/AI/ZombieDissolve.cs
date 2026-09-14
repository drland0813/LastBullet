using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastBullet
{
    public class ZombieDissolve : MonoBehaviour
    {
        [Header("Dissolve")]
        [SerializeField] private Material _dissolveTemplate;
        [SerializeField] private float _dissolveFrom = 1.27f;
        [SerializeField] private float _dissolveTo = -0.5f;

        private Renderer[] _renderers;
        private List<Material> _dissolveMaterials = new List<Material>();
        private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private Coroutine _dissolveCoroutine;

        private static readonly int _CutoffHeightId = Shader.PropertyToID("_CutoffHeight");
        private static readonly int _BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int _BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _ColorId = Shader.PropertyToID("_Color");

        public bool IsDissolving => _dissolveCoroutine != null;

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnDestroy()
        {
            if (_dissolveCoroutine != null)
            {
                StopCoroutine(_dissolveCoroutine);
                _dissolveCoroutine = null;
            }

            ClearDissolveMaterials();
        }

        public void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalMaterials.Clear();

            foreach (Renderer currentRenderer in _renderers)
            {
                if (currentRenderer == null) continue;
                _originalMaterials[currentRenderer] = currentRenderer.sharedMaterials;
            }
        }

        public void RestoreOriginalMaterials()
        {
            if (_dissolveCoroutine != null)
            {
                StopCoroutine(_dissolveCoroutine);
                _dissolveCoroutine = null;
            }

            foreach (KeyValuePair<Renderer, Material[]> pair in _originalMaterials)
            {
                if (pair.Key == null) continue;
                pair.Key.sharedMaterials = pair.Value;
            }

            ClearDissolveMaterials();
        }

        public IEnumerator DissolveOverTime(float dissolveDuration)
        {
            if (_dissolveCoroutine != null) yield break;
            _dissolveCoroutine = StartCoroutine(RunDissolve(dissolveDuration));
            yield return _dissolveCoroutine;
        }

        private IEnumerator RunDissolve(float dissolveDuration)
        {
            if (_dissolveTemplate == null || _renderers == null || _renderers.Length == 0)
            {
                _dissolveCoroutine = null;
                yield break;
            }

            SwapToDissolve();
            SetCutoff(_dissolveFrom);

            if (dissolveDuration <= 0f)
            {
                SetCutoff(_dissolveTo);
                _dissolveCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveDuration);
                SetCutoff(Mathf.Lerp(_dissolveFrom, _dissolveTo, t));
                yield return null;
            }

            SetCutoff(_dissolveTo);
            _dissolveCoroutine = null;
        }

        private void SwapToDissolve()
        {
            ClearDissolveMaterials();

            foreach (Renderer currentRenderer in _renderers)
            {
                if (currentRenderer == null) continue;

                Material[] sourceMaterials = currentRenderer.sharedMaterials;
                Material[] newMaterials = new Material[sourceMaterials.Length];

                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material source = sourceMaterials[i];
                    Material instanced = new Material(_dissolveTemplate);
                    CopyAlbedo(source, instanced);
                    newMaterials[i] = instanced;
                    _dissolveMaterials.Add(instanced);
                }

                currentRenderer.materials = newMaterials;
            }
        }

        private void CopyAlbedo(Material source, Material destination)
        {
            if (source == null || destination == null) return;

            if (source.HasProperty(_BaseMapId) && destination.HasProperty(_BaseMapId))
            {
                destination.SetTexture(_BaseMapId, source.GetTexture(_BaseMapId));
                destination.SetTextureOffset(_BaseMapId, source.GetTextureOffset(_BaseMapId));
                destination.SetTextureScale(_BaseMapId, source.GetTextureScale(_BaseMapId));
            }

            if (source.HasProperty(_BaseColorId) && destination.HasProperty(_BaseColorId))
            {
                destination.SetColor(_BaseColorId, source.GetColor(_BaseColorId));
            }
            else if (source.HasProperty(_ColorId) && destination.HasProperty(_BaseColorId))
            {
                destination.SetColor(_BaseColorId, source.GetColor(_ColorId));
            }
        }

        private void SetCutoff(float value)
        {
            foreach (Material currentMaterial in _dissolveMaterials)
            {
                if (currentMaterial == null) continue;
                if (!currentMaterial.HasProperty(_CutoffHeightId)) continue;
                currentMaterial.SetFloat(_CutoffHeightId, value);
            }
        }

        private void ClearDissolveMaterials()
        {
            foreach (Material currentMaterial in _dissolveMaterials)
            {
                if (currentMaterial == null) continue;
                Destroy(currentMaterial);
            }
            _dissolveMaterials.Clear();
        }
    }
}
