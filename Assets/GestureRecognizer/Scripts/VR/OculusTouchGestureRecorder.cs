using GestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GestureRecognizer.OculusAnchorListener))]
public class OculusTouchGestureRecorder : MonoBehaviour
{
    public enum GestureRecorderType
    {
        GestureComposer,
        GestureMatching
    }

    public OVRInput.Controller ControllerType = OVRInput.Controller.LTouch;
    public IGestureRecorder GestureRecorderInstance = null;
    public GestureRecorderType RecorderType = GestureRecorderType.GestureComposer;
    public GameObject Anchor = null;

    private bool m_triggerButtonDown = false;

    // Visualization State

    void Start()
    {
        if (Anchor == null)
        {
            Anchor = gameObject;
        }

        if (RecorderType == GestureRecorderType.GestureComposer)
        {
            GestureRecorderInstance = GameObject.FindObjectOfType<GestureComposer>();
        }
        else
        {
            GestureRecorderInstance = GameObject.FindObjectOfType<GestureMatchingController>();
        }

        if (GestureRecorderInstance == null)
        {
            Debug.LogError("No instance of GestureComposer or GestureMatching found. Check to make sure prefab is active in scene.");
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
    }


    private void TriggerDown()
    {
        GestureRecorderInstance.StartRecording(Anchor);
    }

    private void TriggerUp()
    {
        GestureRecorderInstance.StopRecording();
    }
}
