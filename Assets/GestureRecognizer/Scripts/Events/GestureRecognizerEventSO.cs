using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace GestureRecognizer
{
    [CreateAssetMenu(menuName = "GestureMatcher/Events/Gesture Recognizer Event Channel")]
    public class GestureRecognizerEventSO : ScriptableObject
    {
        public Action<GestureMatcherResult> OnLoadingRequested;
        public void RaiseEvent(GestureMatcherResult matchResult)
        {
            if (OnLoadingRequested != null)
            {
                OnLoadingRequested.Invoke(matchResult);
            }
        }
    }
}
