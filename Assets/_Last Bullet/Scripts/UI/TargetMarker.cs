using UnityEngine;

namespace LastBullet
{
    public class TargetMarker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AimController _aimController;
        [SerializeField] private Transform _markerRoot;

        [Header("Motion")]
        [SerializeField] [Min(0f)] private float _groundOffset = 0.05f;
        [SerializeField] private float _spinSpeed = 90f;

        [Header("Fallback Visual")]
        [Tooltip("Auto-create a flat ring if the marker has no renderer.")]
        [SerializeField] private bool _createFallbackRing = true;
        [SerializeField] [Min(0.01f)] private float _ringInnerRadius = 0.4f;
        [SerializeField] [Min(0.01f)] private float _ringOuterRadius = 0.6f;
        [SerializeField] private Color _ringColor = Color.yellow;

        private Vector3 _visibleScale;
        private bool _isHidden = true;

        private void Awake()
        {
            if (_aimController == null)
            {
                _aimController = FindFirstObjectByType<AimController>();
            }

            if (_markerRoot == null)
            {
                _markerRoot = transform;
            }

            if (_createFallbackRing && _markerRoot.GetComponentInChildren<Renderer>(true) == null)
            {
                CreateFallbackRing();
            }

            _visibleScale = _markerRoot.localScale;
            _isHidden = true;
            _markerRoot.localScale = Vector3.zero;
        }

        private void LateUpdate()
        {
            Transform target = _aimController != null ? _aimController.CurrentTarget : null;

            if (target == null)
            {
                SetHidden(true);
                return;
            }

            SetHidden(false);
            _markerRoot.position = target.position + Vector3.up * _groundOffset;
            _markerRoot.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.Self);
        }

        private void SetHidden(bool hidden)
        {
            if (_isHidden == hidden || _markerRoot == null) return;

            _isHidden = hidden;
            _markerRoot.localScale = hidden ? Vector3.zero : _visibleScale;
        }

        private void CreateFallbackRing()
        {
            const int segments = 48;

            Vector3[] vertices = new Vector3[segments * 4];
            int[] triangles = new int[segments * 12];

            for (int i = 0; i < segments; i++)
            {
                float angleA = (float)i / segments * Mathf.PI * 2f;
                float angleB = (float)(i + 1) / segments * Mathf.PI * 2f;

                Vector3 innerA = new Vector3(Mathf.Cos(angleA) * _ringInnerRadius, 0f, Mathf.Sin(angleA) * _ringInnerRadius);
                Vector3 outerA = new Vector3(Mathf.Cos(angleA) * _ringOuterRadius, 0f, Mathf.Sin(angleA) * _ringOuterRadius);
                Vector3 innerB = new Vector3(Mathf.Cos(angleB) * _ringInnerRadius, 0f, Mathf.Sin(angleB) * _ringInnerRadius);
                Vector3 outerB = new Vector3(Mathf.Cos(angleB) * _ringOuterRadius, 0f, Mathf.Sin(angleB) * _ringOuterRadius);

                int v = i * 4;
                vertices[v] = innerA;
                vertices[v + 1] = outerA;
                vertices[v + 2] = innerB;
                vertices[v + 3] = outerB;

                int t = i * 12;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
                triangles[t + 6] = v;
                triangles[t + 7] = v + 1;
                triangles[t + 8] = v + 2;
                triangles[t + 9] = v + 1;
                triangles[t + 10] = v + 3;
                triangles[t + 11] = v + 2;
            }

            Mesh ringMesh = new Mesh();
            ringMesh.vertices = vertices;
            ringMesh.triangles = triangles;
            ringMesh.RecalculateNormals();
            ringMesh.RecalculateBounds();

            GameObject ringObject = new GameObject("TargetRing");
            ringObject.transform.SetParent(_markerRoot, false);
            ringObject.transform.localPosition = Vector3.zero;
            ringObject.transform.localRotation = Quaternion.identity;
            ringObject.transform.localScale = Vector3.one;

            MeshFilter filter = ringObject.AddComponent<MeshFilter>();
            filter.sharedMesh = ringMesh;

            MeshRenderer renderer = ringObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateRingMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Debug.Log("[TargetMarker] No visual assigned. Created fallback ring.", this);
        }

        private Material CreateRingMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", _ringColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", _ringColor);
            }
            return material;
        }
    }
}
