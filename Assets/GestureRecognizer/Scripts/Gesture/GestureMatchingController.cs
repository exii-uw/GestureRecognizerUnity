using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GestureRecognizer
{
    public class GestureMatcherResult
    {
        public GestureMatcher Gesture;
        public float LikeliHood;
        public float Weight;
        public float RatioWeight;
    }


    [RequireComponent(typeof(GestureComposer))]
    public class GestureMatchingController : MonoBehaviour, IGestureRecorder, IUserEyePositionListener
    {
        ///////////////////////////////////////////////////////////////////////
        /// LM OPTIMIZATION CONTROLS
        ///////////////////////////////////////////////////////////////////////
        [Header("Optimizer Options")]
        [Range(0.0f, 1.0f)]
        public float Sensitivity = 0.001f;

        [Range(0.0f, 1.0f)]
        public float RMSErrorThreshold = 0.001f;

        [Range(0, 100)]
        public int MaxIterations = 10;

        [Range(0.0f, 1.0f)]
        public float SubSamplePrecent = 1.0f;

        // Likelihood calculation
        [Range(1, 100)]
        public float HeatKernalBeta = 50.0f;
        public bool DoFinalGestureMatchingPassOnComplete = false;


        // Visualization
        [Header("Gesture Visualization")]
        public bool EnableGestureVisualization = true;
        [Range(1, 50)]
        public float GestureLineThickness = 10; // mm
        public Color[] GestureColors = new Color[4];


        // Path Options
        [Header("User Path")]
        public bool EnableUserPathVisualization = true;
        [Range(0, 0.02f)]
        public float MinimumPointDistance = 0.005f; // m


        [Header("Result Events")]
        [SerializeField] private GestureRecognizerEventSO m_gestureMatcherRestultEvent = default;

        ///////////////////////////////////////////////////////////////////////
        /// PRIVATE VARS
        ///////////////////////////////////////////////////////////////////////
        private GestureComposer m_gestureComposer = null;
        private GameObject m_pathAnchor = null;
        private Gesture m_currentPath = new Gesture("UserPath");
        private GestureVisualizer m_pathVisualizer = null;
        private GameObject m_initialGestureSateGO = null;

        // Matcher
        private List<GestureMatcher> m_gestureMatchers = new List<GestureMatcher>();
        private List<ConstrainedGestureMatcher.GestureMatchResults> m_matcherResults;
        private List<float> m_likelihoods;
        private List<float> m_weights;
        private float m_weightSum = 0;
        // State 
        bool m_paused = false;

        // Start is called before the first frame update
        void Start()
        {
            // ensure at origin
            gameObject.transform.position = Vector3.zero;
            gameObject.transform.rotation = Quaternion.identity;

            // Get Composer that contains predefined gestures
            m_gestureComposer = gameObject.GetComponent<GestureComposer>();

            // Setup visualization for path
            m_pathVisualizer = GetComponentInChildren<GestureVisualizer>();
            m_pathVisualizer.ShowLabels = false;
            m_pathVisualizer.GestureColor = Color.white;
            m_pathVisualizer.LineThickness = 6.0f;

            m_pathVisualizer.SetGesture(m_currentPath);

            // Create gesture matchers for each predefined gesture in composer
            foreach (var g in m_gestureComposer.GestureSet.Gestures)
            {
                var go = new GameObject("GestureMatcher_" + g.Name);
                go.transform.parent = transform;
                go.transform.position = Vector3.zero;
                go.transform.rotation = Quaternion.identity;

                var gmatcher = go.AddComponent<GestureMatcher>();

                gmatcher.AssignedGesture = g;
                m_gestureMatchers.Add(gmatcher);
            }

            m_matcherResults = new List<ConstrainedGestureMatcher.GestureMatchResults>(
                new ConstrainedGestureMatcher.GestureMatchResults[m_gestureMatchers.Count]);
            m_likelihoods = new List<float>(new float[m_gestureMatchers.Count]);
            m_weights = new List<float>(new float[m_gestureMatchers.Count]);
        }

        // Update is called once per frame
        void Update()
        {
            // Propogate parameters
            for (int i = 0; i < m_gestureMatchers.Count; ++i)
            {
                var matcher = m_gestureMatchers[i];
                matcher.m_matcher.MaxIterations = MaxIterations;
                matcher.m_matcher.Sensitivity = Sensitivity;
                matcher.m_matcher.SubSamplePrecent = SubSamplePrecent;
                matcher.m_matcher.RMSErrorThreshold = RMSErrorThreshold;
                matcher.LineThickness = GestureLineThickness;
                matcher.GestureColor = GestureColors[i % GestureColors.Length];
                matcher.EnableVisualization = EnableGestureVisualization;
            }

            // Update and process path
            if (m_pathAnchor && !m_paused)
            {
                var anchor = m_pathAnchor.transform.position;

                var p = m_currentPath.GetPath()[m_currentPath.GetPath().Count - 1];
                if (Vector3.Distance(p, anchor) > MinimumPointDistance)
                {
                    m_currentPath.Add(anchor);
                }

                if (m_currentPath.GetPath().Count > 5)
                {
                    UpdateAllMatchers();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        /// PUBLIC API
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Interface Overrides
        /// </summary>
        public void StartRecording(GameObject _anchor)
        {
            if (m_initialGestureSateGO == null)
            {
                m_initialGestureSateGO = new GameObject("Initial Transform");
                m_initialGestureSateGO.transform.parent = gameObject.transform;
            }

            // Set Path
            m_currentPath.Clear();

            var anchor = _anchor.transform.position;
            m_currentPath.Add(anchor);
            m_pathAnchor = _anchor;

            // Initial State
            m_initialGestureSateGO.transform.position = _anchor.transform.position;
            m_initialGestureSateGO.transform.rotation = _anchor.transform.rotation;
            m_initialGestureSateGO.transform.localScale = _anchor.transform.localScale;

            // Get Y-Axis rotations
            foreach (var matcher in m_gestureMatchers)
            {
                matcher.ResetState(m_initialGestureSateGO);
            }

            for (int i = 0; i < m_likelihoods.Count; ++i)
            {
                m_likelihoods[i] = 0;
                m_weights[i] = 0;
                m_likelihoods[i] = 0;
            }
            m_pathVisualizer.GlobalAlpha = EnableUserPathVisualization ? 0.7f : 0;

        }

        public void StopRecording()
        {
            m_pathAnchor = null;

            // Calculate results and notify listeners
            if (DoFinalGestureMatchingPassOnComplete)
            {
                StartCoroutine(DoFinalGesturePass(() =>
                {
                    CalculateFinalResults();
                }));
            }
            else
            {
                CalculateFinalResults();
            }
        }

        public void Pause(bool state)
        {
            if (m_initialGestureSateGO == null)
            {
                m_paused = false;
                return;
            }

            m_paused = state;

            if (!m_paused)
            {
                // Update user path to new coordinate system
                m_currentPath.TransformPointsInPlace(gameObject.transform);

                var pos = m_initialGestureSateGO.transform.position;
                m_initialGestureSateGO.transform.position = gameObject.transform.TransformPoint(pos);

                var rot = m_initialGestureSateGO.transform.rotation;
                m_initialGestureSateGO.transform.rotation = gameObject.transform.rotation * rot;

                // Update States
                foreach (var matcher in m_gestureMatchers)
                {
                    matcher.UpdateState(m_initialGestureSateGO);
                }

                // Update visualizer
                m_pathVisualizer.RefreshMesh();

                // Reset parent
                gameObject.transform.position = Vector3.zero;
                gameObject.transform.rotation = Quaternion.identity;

            }

        }



        public void SetEyePosition(Vector3 _eyePosition)
        {
            foreach (var matcher in m_gestureMatchers)
            {
                matcher.GetVisualizer().SetEyeAnchor(_eyePosition);
            }
        }



        /////////////////////////////////////////////////////////////////////////////
        /// INTERNAL
        /////////////////////////////////////////////////////////////////////////////

        IEnumerator DoFinalGesturePass(Action cb)
        {
            // Get Y-Axis rotations
            foreach (var matcher in m_gestureMatchers)
            {
                matcher.ResetState(m_initialGestureSateGO);
            }


            for (int i = 0; i < 10; ++i)
            {
                UpdateAllMatchers();

                yield return new WaitForEndOfFrame();
            }

            cb?.Invoke();
        }

        private void CalculateFinalResults()
        {
            // Get matcher with highest probablity
            int maxIndex = 0;
            for (int i = 0; i < m_likelihoods.Count; ++i)
            {
                if (m_likelihoods[i] >= 1.0)
                {
                    maxIndex = i;
                }
            }

            // Prepare results
            GestureMatcherResult result = new GestureMatcherResult
            {
                Gesture = m_gestureMatchers[maxIndex],
                LikeliHood = m_likelihoods[maxIndex],
                Weight = m_weights[maxIndex],
                RatioWeight = m_weights[maxIndex] / m_weightSum

            };

            // Notify all listeners
            if (m_gestureMatcherRestultEvent != null)
                m_gestureMatcherRestultEvent.RaiseEvent(result);


            // Hide all matchers
            foreach (var matcher in m_gestureMatchers)
            {
                matcher.UpdateState(m_initialGestureSateGO);
                matcher.Hide();
            }
            m_pathVisualizer.GlobalAlpha = 0;
        }

        private void UpdateAllMatchers()
        {
            // Tally total RMSE
            float sumrmse = 0;
            float sumweight = 0;
            for (int i = 0; i < m_gestureMatchers.Count; ++i)
            {
                m_matcherResults[i] = m_gestureMatchers[i].GetResults();
                float rmse = (float)m_matcherResults[i].RMSE * 1000.0f; // convert RMSE to millimeters
                sumrmse += rmse;

                // Calculate weight based on Radial Basis Function (RBF) heat kernel
                m_weights[i] = Mathf.Exp(-(rmse * rmse) / (HeatKernalBeta * HeatKernalBeta));
                sumweight += m_weights[i];
            }
            m_weightSum = sumweight;

            // Update Likelihoods
            for (int i = 0; i < m_gestureMatchers.Count; ++i)
            {
                m_likelihoods[i] = (float)m_weights[i] / m_weights.Max();
            }

            // Update matchers
            for (int i = 0; i < m_gestureMatchers.Count; ++i)
            {
                m_gestureMatchers[i].UpdateLikelihood(m_likelihoods[i]);
            }

            // Process Path
            for (int i = 0; i < m_gestureMatchers.Count; ++i)
            {
                m_gestureMatchers[i].ProcessPath(m_currentPath);
            }
        }

    }
}