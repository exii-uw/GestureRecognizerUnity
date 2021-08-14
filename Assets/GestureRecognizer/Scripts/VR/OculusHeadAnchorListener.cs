
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GestureRecognizer
{
    public enum OVRHeadType
    { 
        CenterEye
    }


    public class OculusHeadAnchorListener : MonoBehaviour
    {
        //public OVRInput.E ControllerType = OVRInput.Controller.LTouch;
        public OVRHeadType HeadType = OVRHeadType.CenterEye;
        public GameObject Anchor = null;

        // Start is called before the first frame update
        void Start()
        {
            if (Anchor == null)
            {
                string headType = HeadType == OVRHeadType.CenterEye ? "CenterEyeAnchor" : "CenterEyeAnchor";
                Anchor = GameObject.Find(headType);
            }
           
        }

        // Update is called once per frame
        void Update()
        {
            if (Anchor)
            {
                transform.position = Anchor.transform.position;
                transform.rotation = Anchor.transform.rotation;
            }

        }
    }

}