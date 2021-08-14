using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OVR;
using MathNet.Numerics;
using System;
using System.IO;

namespace GestureRecognizer
{
    // Process and updates a gesture based on the current position of object attached. 
    public class GestureComposer : MonoBehaviour, IGestureRecorder
    {
        public GestureGroup GestureSet = new GestureGroup();

        [HideInInspector]
        public string ActiveGestureSet = "";


        // Private Vars
        private bool m_recordGesture = false;
        private GameObject RecorderAnchor = null;


        void Awake()
        {
            UpdateGestureSet();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (m_recordGesture)
            {
                Gesture gesture = GestureSet.GetLastGesture();
                if (gesture != null)
                    gesture.Add(m_initialRecorderGO.transform.InverseTransformPoint(RecorderAnchor.transform.position));
            }
        
        }

        /////////////////////////////////////////////////////////////////////////////
        /// PUBLIC API
        /////////////////////////////////////////////////////////////////////////////




        public void AddGesture(Gesture _gesture = null)
        {
            Gesture tmp = _gesture;
            float rangeDegree = 22.5f;

            if (tmp == null)
            {
                tmp = new Gesture("Unknown_" + GestureSet.Gestures.Count, new GestureOptions
                {
                    // Min / Max in Meters
                    ScaleUniform = new GestureOptionsEntry { Enabled = true, MinRange = 0.2f, MaxRange = 2 },

                    // Min / Max in Degress
                    RotationX = new GestureOptionsEntry { Enabled = true, MinRange = -rangeDegree, MaxRange = rangeDegree },
                    RotationY = new GestureOptionsEntry { Enabled = true, MinRange = -rangeDegree, MaxRange = rangeDegree },
                    RotationZ = new GestureOptionsEntry { Enabled = true, MinRange = -rangeDegree, MaxRange = rangeDegree },

                    // Min / Max in Meters
                    TranslationX = new GestureOptionsEntry { Enabled = true, MinRange = -1.0f, MaxRange = 1.0f },
                    TranslationY = new GestureOptionsEntry { Enabled = true, MinRange = -1.0f, MaxRange = 1.0f },
                    TranslationZ = new GestureOptionsEntry { Enabled = true, MinRange = -1.0f, MaxRange = 1.0f },
                });
            }
            GestureSet.Add(tmp);
        }

        public void Undo()
        {
            GestureSet.Undo();
        }

        public List<string> GetListofGestureNames()
        {
            return GestureSet.GetNames();
        }

        /// <summary>
        /// Interface Overrides
        /// </summary>
        private GameObject m_initialRecorderGO = null;
        public void StartRecording(GameObject _anchor)
        {
            if (m_initialRecorderGO == null)
            {
                m_initialRecorderGO = new GameObject("TempInitialState");
            }

            m_initialRecorderGO.transform.position = _anchor.transform.position;
            m_initialRecorderGO.transform.rotation = _anchor.transform.rotation;

            RecorderAnchor = _anchor;
            StartGestureRecording();
        }

        public void StopRecording()
        {
            StopGestureRecording();
        }

        /////////////////////////////////////////////////////////////////////////////
        /// INTERNAL
        /////////////////////////////////////////////////////////////////////////////

        private void StartGestureRecording()
        {
            Gesture gesture = GestureSet.GetLastGesture();
            if (gesture == null)
            {
                Debug.LogError("No gesture present");
                return;
            }

            gesture.Clear();
            m_recordGesture = true;
        }

        private void StopGestureRecording()
        {
            m_recordGesture = false;

            // Noramlize Path
            Gesture gesture = GestureSet.GetLastGesture();

            if (gesture != null)
                gesture.NormalizePathInPlace();
        }

        public void Pause(bool state)
        {
            ;
        }


        /////////////////////////////////////////////////////////////////////////////
        /// EDITOR
        /////////////////////////////////////////////////////////////////////////////


        public void UpdateGestureSet()
        {
            if (string.IsNullOrEmpty(ActiveGestureSet))
            {
                Debug.LogWarning("Import Gesture from Resource Folder or create your own");
                return;
            }

            var path = Path.Combine(Application.streamingAssetsPath, $"{ActiveGestureSet}.json");
            LoadGestureSet(path);
        }

        public void LoadGestureSet(string _path)
        {
            if (!File.Exists(_path)) return;

            var data = File.ReadAllText(_path);
            GestureSet = JsonUtility.FromJson<GestureGroup>(data);
            GestureSet.UpdateLinks();
        }



    }
}
