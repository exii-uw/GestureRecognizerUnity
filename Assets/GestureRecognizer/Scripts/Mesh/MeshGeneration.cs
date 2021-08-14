using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace GestureRecognizer
{

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshGeneration : MonoBehaviour
    {
        private Mesh mesh;
        private Texture2D uvmap;

        public Vector3[] vertices;

        public int Width = 640;
        public int Height = 480;

        public float Depth = 4.0f;
        public float Stride = 2.0f;
        public float Heat = 0.2f;

        void Start()
        {
            GenerateMesh(Width, Height);
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.P))
            {
                GenerateMesh(Width, Height);
            }
        }

        public void GenerateMesh()
        {
            GenerateMesh(Width, Height);
        }

        public void GenerateMesh(int width, int height)
        {
            Width = width;
            Height = height;
            Assert.IsTrue(SystemInfo.SupportsTextureFormat(TextureFormat.RGFloat));
            uvmap = new Texture2D(width, height, TextureFormat.RGFloat, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
            GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_UVMap", uvmap);

            if (mesh != null)
                mesh.Clear();
            else
                mesh = new Mesh()
                {
                    indexFormat = IndexFormat.UInt32,
                };

            vertices = new Vector3[width * height];
            for (int i = 0; i < vertices.Length; ++i)
            {
                float x = i % width;
                float y = (float)Math.Floor((float)i / width);

                float xn = (x / width) * 2.0f * (width / height) - 1.0f;
                float yn = (y / height) * 2.0f - 1.0f;
                float d = Depth + (Heat * (float)(Math.Sin(xn * Math.PI * Stride) * Math.Cos(yn * Math.PI * Stride)));

                Vector3 P = new Vector3(xn / 2, yn / 2, 1);
                vertices[i] = P * d;
            }

            var indices = new int[vertices.Length * 6 - 6 * width];
            int index = 0;
            for (int i = 0; i < (vertices.Length - width - 1) / 1; i += 1)
            {
                if (i % width >= width - 1) continue;

                var v1 = i;
                var v2 = i + width;
                var v3 = i + width + 1;
                var v4 = i + 1;

                var d1 = vertices[v1].magnitude;
                var d2 = vertices[v2].magnitude;
                var d3 = vertices[v3].magnitude;
                var d4 = vertices[v4].magnitude;

                indices[index++] = v1;
                indices[index++] = v3;
                indices[index++] = v2;
                indices[index++] = v1;
                indices[index++] = v4;
                indices[index++] = v3;
            }

            var uvs = new Vector2[vertices.Length];
            Array.Clear(uvs, 0, uvs.Length);
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    uvs[i + j * width].x = i / (float)width;
                    uvs[i + j * width].y = j / (float)height;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void OnDestroy()
        {
            if (mesh != null)
                Destroy(null);
        }

        private void Dispose()
        {
        }

    }
}
