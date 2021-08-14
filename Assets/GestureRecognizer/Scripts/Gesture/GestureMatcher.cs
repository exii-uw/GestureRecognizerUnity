//#define _DEBUG_SPHERE //#define _DEBUG_SPHERE 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GestureRecognizer
{
    public class GestureMatcher : MonoBehaviour
    {
        public Gesture AssignedGesture = null;

        [Range(0, 1)]
        public float LikelihoodThreshold = 0.85f;

        [Range(0, 0.5f)]
        public float CompletenessUpdateThreshold = 0.25f;

        // Visualizations
        [Space(10)]
        public GameObject GestureVisualizerGO = null;

        public bool EnableVisualization = true;
        public Color GestureColor = Color.blue;
        public float LineThickness = 10; // mm

        /////////////////////////////////////////////////////////////////////////////
        /// PRIVATE VARS
        /////////////////////////////////////////////////////////////////////////////
        internal ConstrainedGestureMatcher m_matcher;

        private List<Vector3> m_path = new List<Vector3>();
        private ConstrainedGestureMatcher.TransformationParameters m_initialStartState;
        private GestureVisualizer m_gestureVisualizer = null;
        private float m_likelihood = 0;
        private float m_completeness = 0;

        private GameObject m_debugSphere;


        void Awake()
        {

        }

        // Start is called before the first frame update
        void Start()
        {
            if (AssignedGesture == null)
            {
                Debug.LogError("A gesture needs to be assigned to the matcher. Check " + gameObject.name + " properties");
            }

            // Setup matcher
            m_matcher = new ConstrainedGestureMatcher(AssignedGesture);

            GestureVisualizerGO = new GameObject("Visualizer_" + AssignedGesture.Name);
            GestureVisualizerGO.transform.parent = transform;
            GestureVisualizerGO.transform.position = Vector3.zero;
            GestureVisualizerGO.transform.rotation = Quaternion.identity;

            m_gestureVisualizer = GestureVisualizerGO.AddComponent<GestureVisualizer>();
            m_gestureVisualizer.SetGesture(AssignedGesture);


#if _DEBUG_SPHERE
            m_debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_debugSphere.transform.localScale = Vector3.one * 0.025f;
#endif

        }

        // Update is called once per frame
        void Update()
        {
            // Update object to visualize
            {
                UpdateInitialState();

                var TParams = m_matcher.GetResults().Params;
                Vector3 p = ConstrainedGestureMatcher.TransformPoint(TParams, Vector3.zero);
                GestureVisualizerGO.transform.position = ConstrainedGestureMatcher.TransformPoint(m_initialStartState, p);
                GestureVisualizerGO.transform.rotation = m_initialStartState.R * TParams.R;
                GestureVisualizerGO.transform.localScale = TParams.s;
            }

            // Calculate precent completed
            if (m_path.Count > 0)
            {
                Vector3 p = m_path[m_path.Count - 1];
                var tmpCompleteness = CalculateCompleteness(p);

                // Only update if completeness is within 10% threshold
                if (Mathf.Abs(tmpCompleteness - m_completeness) < CompletenessUpdateThreshold)
                {
                    m_completeness = tmpCompleteness;
                }
            }

            // Update visualization
            m_gestureVisualizer.GlobalAlpha = EnableVisualization ? m_likelihood : 0;
            m_gestureVisualizer.Completeness = m_completeness;
            m_gestureVisualizer.LineThickness = LineThickness;
            m_gestureVisualizer.GestureColor = GestureColor;
        }

        private float CalculateCompleteness(Vector3 _point)
        {
            float minFound = float.PositiveInfinity;
            float minIndex = 0;

            List<Vector3> gesturePath = AssignedGesture.GetNormalizedPath();
            var TParams = m_matcher.GetResults().Params;

            Vector3 p = Vector3.zero;
            for (int i = 0; i < gesturePath.Count; ++i)
            {
                Vector3 p_i = ConstrainedGestureMatcher.TransformPoint(TParams, gesturePath[i]);
                p_i = ConstrainedGestureMatcher.TransformPoint(m_initialStartState, p_i);
                var d = (p_i - _point).magnitude;
                if (d < minFound)
                {
                    minFound = d;
                    p = p_i;
                    minIndex = i;
                }
            };

#if _DEBUG_SPHERE
            m_debugSphere.transform.position = p;
#endif

            return minIndex / (float)AssignedGesture.GetPath().Count;
        }

        private GameObject m_initialStateGO = null;
        private void UpdateInitialState()
        {
            if (m_initialStateGO != null)
            {
                // Update Visualizer
                m_initialStartState.t = m_initialStateGO.transform.position;
                m_initialStartState.R = m_initialStateGO.transform.rotation;
                m_initialStartState.s = m_initialStateGO.transform.localScale;
            }
        }


        /////////////////////////////////////////////////////////////////////////////
        /// PUBLIC API
        /////////////////////////////////////////////////////////////////////////////
        public void ResetState(GameObject _initialState)
        {
            m_completeness = 0;
            m_likelihood = 1.0f;

            // Update state 
            UpdateState(_initialState);

            // Reset Matcher
            m_matcher.Reset();
        }

        public void Hide()
        {
            m_likelihood = 0;
        }

        public void UpdateState(GameObject _state)
        {
            m_initialStateGO = _state;
        }

        public void ProcessPath(Gesture _path)
        {
            if (m_matcher == null) return;

            m_path = _path.GetPath();

            Vector3 position = m_initialStartState.t;
            Quaternion rotation = m_initialStartState.R;

            // Process path
            //m_matcher.Verbose = true;
            m_matcher.UpdateWeights(_path, position, rotation);
        }

        public float GetCompleteness()
        {
            return m_completeness;
        }

        public double GetRMSError()
        {
            return m_matcher.GetResults().RMSE;
        }

        public ConstrainedGestureMatcher.GestureMatchResults GetResults()
        {
            return m_matcher.GetResults();
        }

        public GestureVisualizer GetVisualizer()
        {
            return m_gestureVisualizer;
        }

        public void UpdateLikelihood(float _likelihood)
        {
            m_likelihood = _likelihood;
        }


    }

}
