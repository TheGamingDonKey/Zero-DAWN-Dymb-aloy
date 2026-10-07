using System.Collections.Generic;
using UnityEngine;

namespace FocusCore
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class FocusVisuals : MonoBehaviour
    {
        public Material pulseMaterial;
        Mesh mesh;
        MaterialPropertyBlock properties;
        MeshRenderer surface;
        public bool Active => surface != null && surface.enabled;
        public Vector3 Origin { get; private set; }

        void Awake()
        {
            mesh = BuildLattice();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            surface = GetComponent<MeshRenderer>();
            surface.sharedMaterial = pulseMaterial;
            surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            surface.receiveShadows = false;
            properties = new MaterialPropertyBlock();
            Stop();
        }

        public void Begin(Vector3 origin)
        {
            Origin = origin;
            transform.SetPositionAndRotation(origin, Quaternion.identity);
            ShowProgress(0);
        }
        public void ShowProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            transform.localScale = Vector3.one * Mathf.Lerp(0.06f, 3.8f, progress);
            float alpha = 0.14f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f,1,progress)));
            properties.SetColor("_BaseColor", new Color(0.615f, 0.388f, 1, alpha));
            surface.SetPropertyBlock(properties);
            surface.enabled = progress < 1;
        }
        public void Stop() { if (surface != null) surface.enabled = false; }
        void OnDisable() { Stop(); }
        void OnDestroy() { if (mesh != null) Destroy(mesh); }

        static Mesh BuildLattice()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int rows = 8, columns = 16;
            Vector3 Point(int row, int col)
            {
                float latitude = Mathf.PI * row / rows;
                float longitude = 2 * Mathf.PI * col / columns;
                return new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
            }
            void Edge(Vector3 a, Vector3 b)
            {
                if ((a-b).sqrMagnitude < 0.000001f) return;
                Vector3 side = Vector3.Cross((a+b).normalized, (b-a).normalized).normalized * 0.0018f;
                int n = vertices.Count;
                vertices.Add(a-side); vertices.Add(a+side); vertices.Add(b-side); vertices.Add(b+side);
                triangles.AddRange(new[] {n,n+1,n+2,n+2,n+1,n+3});
            }
            for (int r=0; r<rows; r++) for (int col=0; col<columns; col++)
            {
                Edge(Point(r,col), Point(r+1,col));
                Edge(Point(r,col), Point(r,col+1));
                Edge(Point(r,col), Point(r+1,col+1));
            }
            var result = new Mesh { name = "OriginalFocusLattice" };
            result.SetVertices(vertices); result.SetTriangles(triangles,0); result.RecalculateBounds();
            return result;
        }
    }
}
