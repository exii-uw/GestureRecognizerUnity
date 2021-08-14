using GestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GestureRecognizer.OculusAnchorListener))]
public class OctoController : MonoBehaviour
{
   
    public OVRInput.Controller ControllerType = OVRInput.Controller.LTouch;
    public IGestureRecorder GestureRecorderInstance = null;

    private bool m_triggerButtonDown = false;
    private bool m_handButtonDown = false;

    // Visualization State
    private AttachGestureVisualizationToObject m_gestureViz = null;

    // Start is called before the first frame updat

    void Start()
    {
      
        GestureRecorderInstance = GameObject.FindObjectOfType<GestureMatchingController>();
        if (GestureRecorderInstance == null)
        {
            Debug.LogError("No instance of GestureComposer or GestureMatching found. Check to make sure prefab is active in scene.");
        }
        m_gestureViz = gameObject.AddComponent<AttachGestureVisualizationToObject>();
    }

    // Update is called once per frame
    void Update()
    {
        // Visualization
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, ControllerType))
        {
            m_handButtonDown = true;
            GestureRecorderInstance.Pause(true);
            m_gestureViz.AttachGestureMatcher();
        }

        if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, ControllerType))
        {
            m_handButtonDown = false;
            GestureRecorderInstance.Pause(false);
            m_gestureViz.DetachGestureMatcher();
        }
    }

}
