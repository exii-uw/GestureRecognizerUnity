using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GestureRecognizer
{
    public interface IGestureRecorder
    {
        void StartRecording(GameObject _anchor);
        void StopRecording();
        void Pause(bool state);

    }


    public interface IUserEyePositionListener
    {
        void SetEyePosition(Vector3 _eyePosition);
    }
}
