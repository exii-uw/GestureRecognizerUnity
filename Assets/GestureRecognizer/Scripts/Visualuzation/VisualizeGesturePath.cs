using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GestureRecognizer
{
    [RequireComponent(typeof(OculusAnchorListener))]
    public class VisualizeGesturePath : MonoBehaviour
    {

        public GestureVisualizer Visualizer = null;
        public OVRInput.Controller ControllerType = OVRInput.Controller.LTouch;
        private Gesture m_temporaryGesture = null;
        private bool m_triggerButtonDown = false;

        // Start is called before the first frame update
        void Start()
        {
            m_temporaryGesture = new Gesture("Temp");
            if (Visualizer != null)
            {
                Visualizer.SetGesture(m_temporaryGesture);
            }
        }

        // Update is called once per frame
        void Update()
        {

            float triggerVal = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, ControllerType);

            if (triggerVal > 0.95f)
            {
                if (!m_triggerButtonDown)
                {
                    TriggerDown();
                    m_triggerButtonDown = true;
                }
            }
            if (triggerVal < 0.05f)
            {
                if (m_triggerButtonDown)
                {
                    TriggerUp();
                    m_triggerButtonDown = false;
                }
            }

            if (m_triggerButtonDown)
            {
                m_temporaryGesture.Add(gameObject.transform.position);
            }

        }


        private void TriggerDown()
        {
            m_temporaryGesture.Clear();
        }

        private void TriggerUp()
        {
        }
    }
}