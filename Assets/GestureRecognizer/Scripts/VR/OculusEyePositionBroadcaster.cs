using GestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GestureRecognizer.OculusHeadAnchorListener))]
public class OculusEyePositionBroadcaster : MonoBehaviour
{

    public IUserEyePositionListener EyePositionListener = null;

    // Start is called before the first frame updat
    void Start()
    {
        EyePositionListener = GameObject.FindObjectOfType<GestureMatchingController>();

        if (EyePositionListener == null)
        {
            Debug.LogError("No instance of GestureComposer or GestureMatching found. Check to make sure prefab is active in scene.");
        }
    }

    // Update is called once per frame
    void Update()
    {
       if(EyePositionListener != null)
        {
            EyePositionListener.SetEyePosition(gameObject.transform.position);
        }
    }
}
