using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
#if UNITY_EDITOR
    public class MeshBuilder
    {
        private List<Vector3>  vertices = new List<Vector3>();
        private List<Vector3> normals = new List<Vector3>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<int> triangles = new List<int>();
        private List<Color> colors = new List<Color>();

        private int startIndex = 0;

        public void StartTriangle() => startIndex = vertices.Count;

        //public int VertexCount => vertices.Count;

        public void AddColor(Color c) => colors.Add(c);
        public void AddVertex(Vector3 v) => vertices.Add(v);
        public void AddNormal(Vector3 n) => normals.Add(n);
        public void AddUV(Vector2 v) => uvs.Add(v);
        public void AddTriangle(int i) => triangles.Add(i);

        public bool HasMesh() => triangles.Count > 0;

        public void AddFullTriangle(Vector3[] vertArray, Vector3[] normalArray, Vector2[] uvArray, Color[] colorArray, int[] triangleArray)
        {
            StartTriangle();
            AddVertices(vertArray);
            AddNormals(normalArray);
            AddUVs(uvArray);
            if(colors != null)
                AddColors(colorArray);

            AddTriangles(triangleArray);
        }


        public void AddVertices(Vector3[] vertArray)
        {
            foreach(var v in vertArray)
                vertices.Add(v);
        }

        public void AddNormals(Vector3[] normalArray)
        {
            foreach(var n in normalArray)
                normals.Add(n);
        }

        public void AddUVs(Vector2[] uvArray)
        {
            foreach(var uv in uvArray)
                uvs.Add(uv);
        }

        public void AddTriangles(int[] triArray)
        {
            foreach(var t in triArray)
                triangles.Add(startIndex + t);
        }

        public void AddColors(Color[] colorArray)
        {
            if (colorArray == null)
                return;
            foreach(var c in colorArray)
                colors.Add(c);
        }
        
        public Mesh Build(string name = "Mesh", bool buildSecondaryUVs = false)
        {
            if (!HasMesh())
                return new Mesh();

            var mesh = new Mesh();
            mesh.name = name;

            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.Optimize();
            mesh.OptimizeIndexBuffers();
            mesh.OptimizeReorderVertexBuffer();

            if(buildSecondaryUVs)
                Unwrapping.GenerateSecondaryUVSet(mesh);

            return mesh;
        }
        

        public MeshBuilder()
        {

        }
    }
#endif
}
