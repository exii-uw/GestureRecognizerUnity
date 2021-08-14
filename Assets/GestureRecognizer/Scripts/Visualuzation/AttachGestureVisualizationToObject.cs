using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace GestureRecognizer
{
    public class AttachGestureVisualizationToObject : MonoBehaviour
    {

        private GestureMatchingController m_getureMatchingController = null;

        // Start is called before the first frame update
        void Start()
        {
            m_getureMatchingController = GameObject.FindObjectOfType<GestureMatchingController>();
        }

        
        public void AttachGestureMatcher()
        {
            m_getureMatchingController.gameObject.transform.parent = gameObject.transform;
        }

        public void DetachGestureMatcher()
        {
            m_getureMatchingController.transform.parent = null;
        }
    }
}