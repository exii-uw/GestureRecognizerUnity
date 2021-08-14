using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GestureRecognizer
{
    public class ConstrainedGestureMatcher
    {
        public delegate void TransformProgressUpdate(GestureMatchResults _tparams);
        public struct TransformationParameters
        {
            public Quaternion R;
            public Vector3 t;
            public Vector3 s;
        }

        public struct GestureMatchResults
        {
            public TransformationParameters Params;
            public double RMSE;
        }

        /////////////////////////////////////////////////////////////////////////////
        /// STATIC CONVIENCE METHODS
        /////////////////////////////////////////////////////////////////////////////

        static public Vector3 InverseTransformPoint(TransformationParameters _param, Vector3 _p)
        {
            // TODO: Scale by uniform scalar (Assumed), change to scale independently for each dimensions
            return  -(Quaternion.Inverse(_param.R) * _param.t) + Quaternion.Inverse(_param.R) * (1.0f/_param.s.x * _p);
        }

        static public Vector3 TransformPoint(TransformationParameters _param, Vector3 _p)
        {
            // TODO: Scale by uniform scalar (Assumed), change to scale independently for each dimensions
            return _param.t + _param.R * (_param.s.x * _p);
        }

        static public TransformationParameters MultipleTransformations(TransformationParameters t1, TransformationParameters t2)
        {
            TransformationParameters T = new TransformationParameters();
            T.t = t1.t + (t1.R * t2.t);
            T.R = t1.R * t2.R;
            
            T.s.x = t1.s.x * t2.s.x;
            T.s.y = t1.s.y * t2.s.y;
            T.s.z = t1.s.z * t2.s.z;

            return  T;

        }


        ///////////////////////////////////////////////////////////////////////
        /// PUBLIC VARS
        ///////////////////////////////////////////////////////////////////////

        // LM Settings
        public int ParticleCount = 10;
        public float RMSErrorThreshold = 0.001f;
        public float RMSErrorMaxReset = 0.5f;
        public float Sensitivity = 0.5f;
        public int MaxIterations = 10;
        public float SubSamplePrecent = 1.0f;
        public bool Verbose = false;
        public bool ProcessingPath = false;


        ///////////////////////////////////////////////////////////////////////
        /// PRIVATE VARS
        ///////////////////////////////////////////////////////////////////////

        // Locks List objects when finding intersection of meshess
        private System.Object m_intersectionListLock = new System.Object();


        private System.Object m_registeredOffsetTransformLock = new System.Object();
        private AutoResetEvent m_RegistrationComplete = new AutoResetEvent(false);
        private AutoResetEvent m_ProgressRegistrationComplete = new AutoResetEvent(false);
        private GestureMatchResults m_currentMatchResults;

        private Gesture m_sourceGesture = null;


        /////////////////////////////////////////////////////////////////////////////
        /// PUBLIC API
        /////////////////////////////////////////////////////////////////////////////


        public ConstrainedGestureMatcher(Gesture _source)
        {
            // Create copy of source path
            m_sourceGesture = _source;
        }

        public GestureMatchResults GetResults()
        {
            return m_currentMatchResults;
        }

        public void UpdateWeights(
            Gesture _model, 
            Vector3 _sourcePosition, 
            Quaternion _sourceRotation)
        {
            if (ProcessingPath) return;

            ProcessingPath = true;

            TransformationParameters ModelTransform;
            ModelTransform.R = _sourceRotation;
            ModelTransform.t = _sourcePosition;
            ModelTransform.s = Vector3.one;
            Vector3[] ModelLocalPoints = _model.GetPath().ToArray();


            // Cache previous result
            GestureMatchResults prevMatchResults = m_currentMatchResults;

            TransformationParameters SourceTransform;
            SourceTransform.R = prevMatchResults.Params.R; 
            SourceTransform.t = prevMatchResults.Params.t; 
            SourceTransform.s = prevMatchResults.Params.s; 
            Vector3[] SourceLocalPoints = m_sourceGesture.GetPath().ToArray();


            try
            {
                var processTransform = Task.Run(() =>
                {
                    GestureMatchResults finalTransform = FindMeshTranform(
                        ModelTransform,
                        ModelLocalPoints,
                        SourceTransform,
                        SourceLocalPoints,
                        (GestureMatchResults _results) =>
                        {
                            lock (m_registeredOffsetTransformLock)
                            {
                                m_currentMatchResults.Params = ConstrainedGestureMatcher.MultipleTransformations(prevMatchResults.Params, _results.Params);
                                m_currentMatchResults.RMSE = _results.RMSE;
                                m_ProgressRegistrationComplete.Set();
                            }
                        });

                    lock (m_registeredOffsetTransformLock)
                    {
                        m_currentMatchResults.Params = ConstrainedGestureMatcher.MultipleTransformations(prevMatchResults.Params, finalTransform.Params);
                        m_currentMatchResults.RMSE = finalTransform.RMSE;
                        ProcessingPath = false;
                        m_RegistrationComplete.Set();
                    }

                });
            }
            catch
            {
                ProcessingPath = false;
                Debug.LogError("Mesh registration unsuccesful");
            }
        }

        public void Reset()
        {
            m_currentMatchResults.Params.R = Quaternion.identity;
            m_currentMatchResults.Params.t = Vector3.zero;
            m_currentMatchResults.Params.s = Vector3.one;
            m_currentMatchResults.RMSE = 0;
        }



        /////////////////////////////////////////////////////////////////////////////
        /// INTERNAL
        /////////////////////////////////////////////////////////////////////////////

        // Optimize Game Mesh Objects
        private GestureMatchResults FindMeshTranform(
            TransformationParameters ModelTransform,
            Vector3[] ModelLocalPoints,
            TransformationParameters SourceTransform,
            Vector3[] SourceLocalPoints,
            TransformProgressUpdate ProgressUpdate
            )
        {
            //  Convert data to homogeneous coordinates. NOTE: Unity does not do this for you. Why?
            TransformationParameters modelT = ModelTransform;
            int modelCount = Mathf.FloorToInt(ModelLocalPoints.Length * SubSamplePrecent);
            Vector4[] model = new Vector4[modelCount];

            Parallel.For(0, model.Length, i =>
            {
                int index = Mathf.FloorToInt((1.0f / SubSamplePrecent) * i);
                model[i] = InverseTransformPoint(modelT, ModelLocalPoints[index]);
                model[i].w = 1.0f;
            });

            TransformationParameters sourceT = SourceTransform;
            int sourceCount = Mathf.FloorToInt(SourceLocalPoints.Length * SubSamplePrecent);
            Vector4[] source = new Vector4[sourceCount];

            Parallel.For(0, source.Length, i =>
            {
                int index = Mathf.FloorToInt((1.0f / SubSamplePrecent) * i);
                source[i] = TransformPoint(sourceT, SourceLocalPoints[i]);
                source[i].w = 1.0f;
            });


            // Preprocess data and remove all point cloud data that is further away then some threshold N. 
            // Find the intersections with the model
            Vector4[] M_pruned = new Vector4[model.Length];
            Vector4[] S_pruned = new Vector4[model.Length];
            
            Parallel.For(0, model.Length, i =>
            {
                Vector4 m_i = model[i];
                Vector4 p = new Vector4();

                float minFound = float.PositiveInfinity;

                for (int j = 0; j < source.Length; ++j)
                {
                    Vector4 s_i = source[j];
                    var d = (s_i - m_i).magnitude;
                    if (d < minFound)
                    {
                        minFound = d;
                        p = s_i;
                    }
                }

                lock (m_intersectionListLock)
                {
                    M_pruned[i] = m_i;
                    S_pruned[i] = p;
                }
            });



            ///////////////////////////////////////////////////////////////////////
            // Process Paths
            ///////////////////////////////////////////////////////////////////////

            // Determin Number of dimensions
            var paramOptions = m_sourceGesture.Options;

            // initial parameters
            var parameters0 = Vector<double>.Build.Dense(m_sourceGesture.ParamCount);
            if (paramOptions.ScaleUniform.Enabled)
                parameters0[m_sourceGesture.ParamCount - 1] = 1; // Default Scale

            System.Random autoRand = new System.Random();

            double rmsError = 1;
            bool registered = false;
            int iter = 0;
            while (!registered)
            {
                Matrix4x4 TransformMat = ConvertParametersToMatrix4(parameters0);

                // Find Closest Points
                Vector4[] X = new Vector4[S_pruned.Length];
                Parallel.For(0, M_pruned.Length, i =>
                {

                    Vector4 m_i = M_pruned[i];
                    Vector4 p = new Vector4();

                    float minFound = float.PositiveInfinity;

                    for (int j = 0; j < S_pruned.Length; ++j)
                    {
                        Vector4 s_i = TransformMat * S_pruned[j];
                        var d = (s_i - m_i).magnitude;
                        if (d < minFound)
                        {
                            minFound = d;
                            p = s_i;
                        }
                    }

                    if (p == Vector4.zero)
                        throw new System.Exception("Value for model pick should not be zero!");

                    X[i] = p;
                });


                // Run LM 
                int nerrors = 3;
                int nvalues = S_pruned.Length * nerrors;
                LevenbergMarquardt.Function f = delegate (Vector<double> parameters)
                {
                    var error = Vector<double>.Build.Dense(nvalues);
                    Matrix4x4 Tn = ConvertParametersToMatrix4(parameters);
                    var regularizer = CalculateConstrainedErrorRegularizer(parameters);

                    Parallel.For(0, S_pruned.Length, i =>
                    {
                        Vector4 m_i = M_pruned[i];
                        Vector4 s_i = X[i];

                        Vector4 s_i_star = Tn * s_i;
                        Vector4 p = m_i - s_i_star;
                        error[i * nerrors    ] = p.x * regularizer;
                        error[i * nerrors + 1] = p.y * regularizer;
                        error[i * nerrors + 2] = p.z * regularizer;
                    });

                    return error;
                };


                var levenbergMarquardt = new LevenbergMarquardt(f, (Vector<double> parameter, double rmse) =>
                {
                    // Updates each iteration of LM loop
                    TransformationParameters tparams = ConvertParameters(parameter);
                    ProgressUpdate(new GestureMatchResults { Params = tparams, RMSE = rmse });
                });


                levenbergMarquardt.maximumIterations = 8;
                levenbergMarquardt.maximumInnerIterations = 100;
                levenbergMarquardt.lambdaIncrement = 2.0f;
                levenbergMarquardt.minimumReduction = 1.0e-12;
                levenbergMarquardt.initialLambda = 0.001;
                levenbergMarquardt.minimumErrorTolerance = RMSErrorThreshold;
                levenbergMarquardt.Sensitivity = Sensitivity;

                rmsError = levenbergMarquardt.Minimize(parameters0);

                // Print Updates to Console
                if (Verbose)
                {
                    Debug.Log($"LM ({ iter.ToString() }) -- Error: {rmsError.ToString()}  State: {levenbergMarquardt.State.ToString()}  Params: {parameters0}");
                }

                if (rmsError < RMSErrorThreshold || iter > MaxIterations)
                {
                    registered = true;
                }

                iter++;
            }
    
            if (Verbose)
                Debug.Log("RMSERROR ("+ m_sourceGesture.Name +"): " + (rmsError).ToString());

            return new GestureMatchResults {
                Params = ConvertParameters(parameters0), 
                RMSE = rmsError
            };
        }


        // Covnert parameters into Transformation Matrix
        private Matrix4x4 ConvertParametersToMatrix4(Vector<double> _params)
        {
            var T = ConvertParameters(_params);
            return Matrix4x4.TRS(T.t, T.R, T.s);
        }

        private TransformationParameters ConvertParameters(Vector<double> _params)
        {
            TransformationParameters T;

            var options = m_sourceGesture.Options;
            int paramIndex = 0;
            Vector3 t = Vector3.zero;

            t.x = (options.TranslationX.Enabled) ? (float)_params[paramIndex++] : 0;
            t.y = (options.TranslationY.Enabled) ? (float)_params[paramIndex++] : 0;
            t.z = (options.TranslationZ.Enabled) ? (float)_params[paramIndex++] : 0;

            Vector3 e = Vector3.zero;
            e.x = (options.RotationX.Enabled) ? (float)_params[paramIndex++] : 0;
            e.y = (options.RotationY.Enabled) ? (float)_params[paramIndex++] : 0;
            e.z = (options.RotationZ.Enabled) ? (float)_params[paramIndex++] : 0;

            Vector3 s = Vector3.one;
            s.x = s.y = s.z = (options.ScaleUniform.Enabled) ? (float)_params[paramIndex++] : 1;

            // Group
            T.t = t;
            T.R = Quaternion.Euler(e);
            T.s = s;

            if (float.IsNaN(T.R.x))
            {
                Debug.LogWarning($"NAN: {e}   Params: [{_params[0]}, {_params[1]}, {_params[2]}, {_params[3]}, {_params[4]}, {_params[5]}, {_params[6]}]");
            }

            return T;
        }


        private double CalculateConstrainedErrorRegularizer(Vector<double> _params)
        {
            double errorRegularizer = 1;
            var options = m_sourceGesture.Options;
            int paramIndex = 0;


            if (options.TranslationX.Enabled)
            {
                errorRegularizer += CalculateError(_params[paramIndex], options.TranslationX);
                paramIndex++;
            }

            if (options.TranslationY.Enabled)
            {
                errorRegularizer += CalculateError(_params[paramIndex], options.TranslationY);
                paramIndex++;
            }

            if (options.TranslationZ.Enabled)
            {
                errorRegularizer += CalculateError(_params[paramIndex], options.TranslationZ);
                paramIndex++;
            }

            if (options.RotationX.Enabled)
            {
                errorRegularizer += CalculateError(_params[paramIndex], options.RotationX);
                paramIndex++;
            }

            if (options.RotationY.Enabled)
            {
                errorRegularizer += CalculateError(_params[paramIndex], options.RotationY);
                paramIndex++;
            }

            if (options.RotationZ.Enabled)
            {
                errorRegularizer += CalculateError(_params[paramIndex], options.RotationZ);
                paramIndex++;
            }

            if (options.ScaleUniform.Enabled)
            {
                errorRegularizer += CalculateScaleError(_params[paramIndex], options.ScaleUniform);
                paramIndex++;
            }

            return errorRegularizer;
        }


        private double CalculateError(double paramVal, GestureOptionsEntry optionsEntry, double multiple = 100, float power = 10)
        {
            if (paramVal < optionsEntry.MinRange)
            {
                var delta = paramVal - optionsEntry.MinRange;
                return Math.Pow(multiple * delta, power);
            }
            
            if (paramVal > optionsEntry.MaxRange)
            {
                var delta = paramVal - optionsEntry.MaxRange;
                return Math.Pow(multiple * delta, power);
            }

            return 0;
        }

        private double CalculateScaleError(double paramVal, GestureOptionsEntry optionsEntry, double multiple = 100, float power = 10)
        {
            if (paramVal < optionsEntry.MinRange)
            {
                var delta = paramVal - optionsEntry.MinRange;
                return 1/paramVal - 1/optionsEntry.MaxRange;
            }

            if (paramVal > optionsEntry.MaxRange)
            {
                var delta = paramVal - optionsEntry.MaxRange;
                return Math.Pow(multiple * delta, power);
            }

            return 0;
        }

    }
}
