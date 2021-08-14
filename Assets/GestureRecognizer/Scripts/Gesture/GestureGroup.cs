using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace GestureRecognizer
{
    [Serializable]
    public class GestureOptionsEntry
    {
        public bool Enabled = false;
        public float MinRange = Single.MinValue;
        public float MaxRange = Single.MaxValue;
    
        public GestureOptionsEntry()
        { }

        public GestureOptionsEntry(bool _enabled)
        {
            Enabled = _enabled;
        }

        public GestureOptionsEntry(bool _enabled, float _minRange, float _maxRange)
        {
            Enabled = _enabled;
            MinRange = _minRange;
            MaxRange = _maxRange;
        }
    }

    [Serializable]
    public class GestureOptions
    {
        public GestureOptionsEntry ScaleUniform = new GestureOptionsEntry();
        public GestureOptionsEntry RotationX = new GestureOptionsEntry();
        public GestureOptionsEntry RotationY = new GestureOptionsEntry();
        public GestureOptionsEntry RotationZ = new GestureOptionsEntry();
        public GestureOptionsEntry TranslationX = new GestureOptionsEntry();
        public GestureOptionsEntry TranslationY = new GestureOptionsEntry();
        public GestureOptionsEntry TranslationZ = new GestureOptionsEntry();
    }

    [Serializable]
    public class Gesture
    {
        public string GUID = Guid.NewGuid().ToString();
        public string Name = "";
        public int ParamCount = 0;

        public GestureOptions Options = new GestureOptions();

        [SerializeField]
        private List<Vector3> Path = new List<Vector3>();

        [SerializeField]
        private List<Vector3> NormalizedPath = new List<Vector3>();

        [SerializeField]
        private Vector3 StartPoint = new Vector3();

        public Gesture(string _name)
        {
            Name = _name;
        }

        public Gesture(string _name, GestureOptions _options)
        {
            Name = _name;
            Options = _options;


        }


        public List<Vector3> GetPath()
        {
            return Path;
        }

        public List<Vector3> GetNormalizedPath()
        {
            return NormalizedPath;
        }

        public void Clear()
        {
            Path.Clear();
            NormalizedPath.Clear();
        }

        public void Add(Vector3 p)
        {
            if (Path.Count == 0)
            {
                StartPoint = p;
            }

            Path.Add(p);
            NormalizedPath.Add(NoramlizePoint(p));
        }



        public void NormalizePathInPlace()
        {
            if (Path == null) return;
            if (Path.Count == 0) return;

            Vector3 startPoint = Path[0];

            for (int i = 0; i < Path.Count; ++i)
            {
                Path[i] -= startPoint;
            }
        }

        public void ProjectPathToXYPlaneInPlace(float _zPos = 0)
        {
            if (Path == null) return;
            if (Path.Count == 0) return;

            for (int i = 0; i < Path.Count; ++i)
            {
                var p = Path[i];
                p.z = _zPos;

                Path[i] = p;
            }
        }

        public void TransformPointsInPlace(Transform T)
        {
            if (Path.Count == 0) return;

            for (int i = 0; i < Path.Count; ++i)
            {
                Path[i] = T.TransformPoint(Path[i]);
            }

            StartPoint = Path[0];
            
            for (int i = 0; i < NormalizedPath.Count; ++i)
            {
                NormalizedPath[i] = NoramlizePoint(Path[i]);
            }
        }

        internal void Initialize()
        {
            NormalizedPath = Path;
            StartPoint = Path[0];
            ParamCount = 0;
            ParamCount += Options.TranslationX.Enabled ? 1 : 0;
            ParamCount += Options.TranslationY.Enabled ? 1 : 0;
            ParamCount += Options.TranslationZ.Enabled ? 1 : 0;
            ParamCount += Options.RotationX.Enabled ? 1 : 0;
            ParamCount += Options.RotationY.Enabled ? 1 : 0;
            ParamCount += Options.RotationZ.Enabled ? 1 : 0;
            ParamCount += Options.ScaleUniform.Enabled ? 1 : 0;
        }

        private Vector3 NoramlizePoint(Vector3 p)
        {
            return p - StartPoint;
        }
    }


    [Serializable]
    public class GestureGroup
    {
        public string GestureSetName = "GestureSetExample";
        private Dictionary<string, int> m_nameToGestureIndex = new Dictionary<string, int>();
        public List<Gesture> Gestures = new List<Gesture>();

        public GestureGroup()
        { }

        public void Add(Gesture _gesture)
        {
            m_nameToGestureIndex.Add(_gesture.Name, Gestures.Count);
            Gestures.Add(_gesture);
        }
        
        public void Undo()
        {
            m_nameToGestureIndex.Remove(GetLastGesture().Name);
            Gestures.RemoveAt(Gestures.Count - 1);
        }

        public Gesture GetLastGesture()
        {
            if (Gestures.Count == 0) return null;
            return Gestures[Gestures.Count - 1];
        }

        public void NormalizeAllGesturesInPlace()
        {
            foreach (var g in Gestures)
            {
                g.NormalizePathInPlace();
            }
        }

        public void ProjectAllPathsXYPlane()
        {
            foreach (var g in Gestures)
            {
                g.ProjectPathToXYPlaneInPlace();
            }
        }

        public List<string> GetNames()
        {
            return new List<string>(m_nameToGestureIndex.Keys);
        }

        public Gesture GetGestureByName(string _name)
        {
            int index = 0;
            if (m_nameToGestureIndex.TryGetValue(_name,  out index))
            {
                return Gestures[index];
            }
            return null;
        }

        // Update private data structures
        internal void UpdateLinks()
        {
            for(int i = 0; i < Gestures.Count; ++i)
            {
                m_nameToGestureIndex.Add(Gestures[i].Name, i);
                Gestures[i].Initialize();
            }
        }
    }


}
