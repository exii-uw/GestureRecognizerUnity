using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace GestureRecognizer
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GestureVisualizer : MonoBehaviour
    {
        public enum LineRendererMaterial
        {
            LineRendererStrip,
            LineRendererTube
        }

        public string GestureName;
        public Color GestureColor = Color.blue;
        public float LineThickness = 4; // mm
        public float Completeness = 1.0f;
        public float Trail = 0.2f;
        public float GlobalAlpha = 1.0f;

        public bool EnableOcto3DVisualization = true;

        // Labels
        [Space(10)]
        public bool ShowLabels = true;

        [HideInInspector]
        public int m_GestureIndex = 0;

        [HideInInspector]
        public GestureComposer GestureComposerInstance = null;

        // Render Type
        [Space(10)]
        public LineRendererMaterial LineRenderer = LineRendererMaterial.LineRendererStrip;



        private Gesture m_gesture = null;
        private Mesh mesh;
        private List<Vector3> vertices;
        private bool m_initialized = false;
        private string m_internalCurrentGestureName = "";
        private Material m_lineMaterial = null;
        
        private GameObject m_label = null;
        private Vector3 m_eyeAnchor;

        void Awake()
        {

        }

        // Start is called before the first frame update
        void Start()
        {
            if (GestureComposerInstance == null)
            {
                GestureComposerInstance = GameObject.FindObjectOfType<GestureComposer>();
            }

            // Setup mesh
            mesh = new Mesh()
            {
                indexFormat = IndexFormat.UInt32,
            };

            UpdateGesture();
            GenerateMesh();

            // Update material
            Shader shader = Shader.Find("Unlit/Texture");
            switch (LineRenderer)
            {
                case LineRendererMaterial.LineRendererStrip:
                    shader = Shader.Find("Gesture/GestureVisualizationLineStrip");
                    break;
                case LineRendererMaterial.LineRendererTube:
                    shader = Shader.Find("Gesture/GestureVisualizationTube");
                    break;
            }

            m_lineMaterial = new Material(shader);
            GetComponent<MeshRenderer>().material = m_lineMaterial;

        }

        // Update is called once per frame
        void Update()
        {
            if (m_initialized && m_gesture.GetPath().Count > 0)
            {
                UpdateMesh();
                UpdateLabel();
            }

            if (m_internalCurrentGestureName != GestureName)
            {
                UpdateGesture();
                GenerateMesh();

            }

           
        }

        public void SetGesture(Gesture _gesture)
        {
            m_GestureIndex = -1;
            GestureName = _gesture.Name;
            m_internalCurrentGestureName = GestureName;
            m_gesture = _gesture;

            if (mesh != null)
            {
                mesh.Clear();
            }

            // Setup Label
            if (ShowLabels)
            {
                m_label = new GameObject("Label");
                m_label.transform.parent = gameObject.transform;
                m_label.transform.localPosition = Vector3.zero;

                var canvas = m_label.AddComponent<Canvas>();

                var tempGO = new GameObject("Text");
                tempGO.transform.parent = m_label.transform;

                Vector3 pos = Vector3.zero;
                pos.y = 0.05f;
                tempGO.transform.localPosition = pos;
                tempGO.transform.localRotation = Quaternion.identity;

                var tmp = tempGO.AddComponent<TextMeshProUGUI>();
                tmp.text = GestureName;
                tmp.fontSize = 0.03f;
                tmp.color = GestureColor;
                tmp.outlineColor = Color.black;
                tmp.outlineWidth = 0.2f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontStyle = FontStyles.Bold;

                Vector2 rect = new Vector2();
                rect.x = 0.2f;
                rect.y = 0.03f;
                m_label.GetComponent<RectTransform>().sizeDelta = rect;
                tempGO.GetComponent<RectTransform>().sizeDelta = rect;
            }


            m_initialized = true;
        }

        public void SetEyeAnchor(Vector3 eyeAnchor)
        {
            m_eyeAnchor = eyeAnchor;
        }

        public void RefreshMesh()
        {
            GenerateMesh();
        }

        /////////////////////////////////////////////////////////////////////////////
        /// INTERNAL
        /////////////////////////////////////////////////////////////////////////////


        // Create Line Mesh using Path
        private void UpdateMesh()
        {
            GetComponent<MeshRenderer>().material.SetColor("_Color", GestureColor);
            GetComponent<MeshRenderer>().material.SetFloat("_GestureCompleteness", Completeness);
            GetComponent<MeshRenderer>().material.SetFloat("_GestureTrail", Trail);
            GetComponent<MeshRenderer>().material.SetFloat("_GestureGlobalAlpha", GlobalAlpha);

            GetComponent<MeshRenderer>().material.SetInt("_Octo3DVisualization", EnableOcto3DVisualization ? 1 : 0);
            GetComponent<MeshRenderer>().material.SetFloat("_LineThickness", EnableOcto3DVisualization ? GlobalAlpha * LineThickness : LineThickness);

            if (mesh.vertices.Length != m_gesture.GetPath().Count)
            {
                GenerateMesh();
            }
        }

        private void UpdateLabel()
        {
            if (m_label == null) return;

            int index = (int) Mathf.Floor((float)m_gesture.GetPath().Count * (Completeness + Trail));
            if (index < 0) index = 0;
            if (index >= m_gesture.GetPath().Count) index = m_gesture.GetPath().Count - 1;

            // Set Position
            Vector3 localPoint = m_gesture.GetPath()[index];
            m_label.transform.localPosition = localPoint;

            // Set Attributes
            var tmp = m_label.GetComponentInChildren<TextMeshProUGUI>();

            var c = GestureColor;
            c.a = GlobalAlpha;
            tmp.color = c;

            // Set rotations
            if (m_eyeAnchor != Vector3.zero)
            {
                m_label.transform.rotation = Quaternion.LookRotation(m_label.transform.position - m_eyeAnchor);
            }

        }

        private void GenerateMesh()
        {
            if (m_gesture == null)
                return;

            if (mesh != null)
            {
                mesh.Clear();
            }

            vertices = m_gesture.GetPath();

            var indices = new int[vertices.Count];
            int index = 0;
            for (int i = 0; i < vertices.Count; i += 1)
            {
                indices[index++] = i;
            }

            var uvs = new Vector2[vertices.Count];
            Array.Clear(uvs, 0, uvs.Length);
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i].x = i / (float)uvs.Length;
                uvs[i].y = i / (float)uvs.Length;
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs;

            List<Vector3> prevVertices = new List<Vector3>(vertices);
            prevVertices.Insert(0, prevVertices[0]);
            prevVertices.RemoveAt(prevVertices.Count - 1);
            mesh.normals = prevVertices.ToArray();

            mesh.SetIndices(indices, MeshTopology.LineStrip, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            GetComponent<MeshFilter>().sharedMesh = mesh;
            m_initialized = true;
        }


        private void UpdateGesture()
        {
            m_internalCurrentGestureName = GestureName;
            var gesture = GestureComposerInstance.GestureSet.GetGestureByName(m_internalCurrentGestureName);
            if (gesture != null)
            {
                m_gesture = gesture;
            }
        }

    }

}