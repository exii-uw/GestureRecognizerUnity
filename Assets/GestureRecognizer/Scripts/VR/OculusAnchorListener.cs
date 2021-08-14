using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GestureRecognizer
{
    public class OculusAnchorListener : MonoBehaviour
    {
        public OVRInput.Controller ControllerType = OVRInput.Controller.LTouch;
        public GameObject Anchor = null;

        // Start is called before the first frame update
        void Start()
        {
            if (Anchor == null)
            {
                string handType = ControllerType == OVRInput.Controller.RTouch ? "RightHandAnchor" : "LeftHandAnchor";
                Anchor = GameObject.Find(handType);
            }
            else
            {
                ControllerType = OVRInput.Controller.None;
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