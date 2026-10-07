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
            // Fast outward wave, then a soft tail. Build geometry once, never per frame.
            transform.localScale = Vector3.one * RadiusAtProgress(progress);
            float alpha = 0.28f * Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.065f,progress))
                * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f,1,progress)));
            properties.SetColor("_BaseColor", new Color(0.615f, 0.388f, 1, alpha));
            properties.SetFloat("_Progress", progress);
            surface.SetPropertyBlock(properties);
            surface.enabled = progress < 1;
        }
        public void Stop() { if (surface != null) surface.enabled = false; }
        public static float RadiusAtProgress(float progress) => Mathf.Lerp(.06f,3.8f,1-Mathf.Pow(1-Mathf.Clamp01(progress),1.35f));
        void OnDisable() { Stop(); }
        void OnDestroy() { if (mesh != null) Destroy(mesh); }

        static Mesh BuildLattice()
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var coordinates = new List<Vector4>();
            const int rows = 12, columns = 24;
            Vector3 Point(int row, int col)
            {
                float latitude = Mathf.PI * row / rows;
                float longitude = 2 * Mathf.PI * col / columns;
                return new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude), Mathf.Cos(latitude), Mathf.Sin(latitude) * Mathf.Sin(longitude));
            }
            void Facet(Vector3 a, Vector3 b, Vector3 c, float seed)
            {
                if (Vector3.Cross(b-a,c-a).sqrMagnitude < .0000001f) return;
                int n = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                coordinates.Add(new Vector4(1,0,0,seed));
                coordinates.Add(new Vector4(0,1,0,seed));
                coordinates.Add(new Vector4(0,0,1,seed));
                triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2);
            }
            for (int r=0; r<rows; r++) for (int col=0; col<columns; col++)
            {
                float seed = Mathf.Repeat((r*columns+col)*.618034f,1);
                Facet(Point(r,col),Point(r+1,col),Point(r+1,col+1),seed);
                Facet(Point(r,col),Point(r+1,col+1),Point(r,col+1),seed);
            }
            var result = new Mesh { name = "OriginalFocusLattice" };
            result.SetVertices(vertices); result.SetUVs(0,coordinates);
            result.SetTriangles(triangles,0); result.RecalculateBounds();
            return result;
        }
    }
}
