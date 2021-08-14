using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestureRecognizer
{

    public class LevenbergMarquardt
    {
        // y_i - f(x_i, parameters) as column vector
        public delegate Vector<double> Function(Vector<double> parameters);
        public delegate void ParameterProgressUpdate(Vector<double> parameters, double rmse);

        public LevenbergMarquardt(Function function, ParameterProgressUpdate _parameterProgress)
            : this(function, _parameterProgress, new NumericalDifferentiation(function).Jacobian)
        {
        }

        // J_ij, ith error from function, jth parameter
        public delegate Matrix<double> Jacobian(Vector<double> parameters);

        public LevenbergMarquardt(Function function, ParameterProgressUpdate _parameterProgress, Jacobian jacobianFunction)
        {
            this.function = function;
            this.jacobianFunction = jacobianFunction;
            this.parameterProgress = _parameterProgress;
        }

        public enum States { Running, MaximumIterations, LambdaTooLarge, ReductionStepTooSmall, MinimumErrorReached };
        public double RMSError { get { return rmsError; } }
        public States State { get { return state; } }

        public int maximumIterations = 100;
        public int maximumInnerIterations = int.MaxValue;
        public double minimumReduction = 1.0e-5;
        public double maximumLambda = 1.0e9;
        public double lambdaIncrement = 10.0;
        public double initialLambda = 1.0e-3;
        public double minimumErrorTolerance = 1.0e-3;
        public double Sensitivity = 0.5f;

        Function function;
        Jacobian jacobianFunction;
        ParameterProgressUpdate parameterProgress;
        States state = States.Running;
        double rmsError;

        public double Minimize(Vector<double> parameters)
        {
            NumericalDifferentiation.Sensitivity = Sensitivity;

            state = States.Running;
            for (int iteration = 0; iteration < maximumIterations; iteration++)
            {
                MinimizeOneStep(parameters);
                if (parameterProgress != null)
                {
                    Vector<double> tmp = Vector<double>.Build.DenseOfVector(parameters);
                    parameterProgress(tmp, RMSError);
                }
                if (state != States.Running)
                    return RMSError;
            }
            state = States.MaximumIterations;
            return RMSError;
        }

        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        public double MinimizeOneStep(Vector<double> parameters)
        {
            // initial value of the function; callee knows the size of the returned vector
            Vector<double> errorVector = function(parameters);
            double error = errorVector.DotProduct(errorVector);

            // Jacobian; callee knows the size of the returned matrix
            var J = jacobianFunction(parameters);

            // J'*J
            var JtJ = J.Transpose() * J;

            // J'*errorVector
            var JtError = J.Transpose() * errorVector;

            // allocate some space
            var JtJaugmented = Matrix<double>.Build.Dense(parameters.Count, parameters.Count);

            // find a value of lambda that reduces error
            double lambda = initialLambda;
            int iteration = 0;
            while (true && iteration < maximumInnerIterations)
            {
                // augment J'*J: J'*J += lambda*(diag(J))
                JtJ.CopyTo(JtJaugmented);
                for (int i = 0; i < parameters.Count(); i++)
                    JtJaugmented[i, i] = (1.0 + lambda) * JtJ[i, i];


                // solve for delta: (J'*J + lambda*(diag(J)))*delta = J'*error
                var JtJinv = JtJaugmented.Inverse();
                var delta = JtJinv * JtError;

                if (double.IsNaN(delta[0]))
                {
                    Console.WriteLine("Delta is NaN");
                    break;

                }

                // Update parameters based on gradient delta
                var newParameters = parameters - delta;

                // evaluate function, compute error
                Vector<double> newErrorVector = function(newParameters);
                double newError = newErrorVector.DotProduct(newErrorVector);

                // if error is reduced, divide lambda by 10
                bool improvement;
                if (newError < error)
                {
                    Console.WriteLine("Imporvement Made : " + newError.ToString() + "   " + error.ToString());
                    lambda /= lambdaIncrement;
                    improvement = true;
                }
                else // if not, multiply lambda by 10
                {
                    lambda *= lambdaIncrement;
                    improvement = false;
                }
                Console.WriteLine("Iteraction " + iteration.ToString() + ": " + error.ToString());

                // termination criteria:
                // reduction in error is too small
                Vector<double> diff = errorVector - newErrorVector;
                double diffSq = diff.DotProduct(diff);
                double errorDelta = Math.Sqrt(diffSq / error);
                double rms = Math.Sqrt(error / errorVector.Count);

                if (rms < minimumErrorTolerance)
                {
                    state = States.MinimumErrorReached;
                }

                if (errorDelta < minimumReduction)
                    state = States.ReductionStepTooSmall;

                // lambda is too big
                if (lambda > maximumLambda)
                    state = States.LambdaTooLarge;

                // change in parameters is too small [not implemented]

                // if we made an improvement, accept the new parameters
                if (improvement)
                {
                    newParameters.CopyTo(parameters);
                    error = newError;
                    break;
                }

                // if we meet termination criteria, break
                if (state != States.Running)
                    break;

                iteration++;
            }

            rmsError = Math.Sqrt(error / errorVector.Count);
            return rmsError;
        }





        public class NumericalDifferentiation
        {
            static public double Sensitivity = 0.5;

            public NumericalDifferentiation(Function function)
            {
                this.function = function;
            }

            private double Lerp(double min, double max, double interval)
            {
                return min + (max - min) * interval;
            }

            // J_ij, ith error from function, jth parameter
            public Matrix<double> Jacobian(Vector<double> parameters)
            {
                const double deltaFactor = 1.0e-8;
                double minDelta = Lerp(1.0e-6, 1.0e-4, Sensitivity);

                // evaluate the function at the current solution
                Vector<double> errorVector0 = function(parameters);
                var J = Matrix<double>.Build.Dense(errorVector0.Count, parameters.Count);

                // vary each paremeter
                for (int j = 0; j < parameters.Count; j++)
                {
                    double parameterValue = parameters[j];
                    //double parameterValue = parameters[j]; // save the original value

                    double delta = parameterValue * deltaFactor;
                    if (Math.Abs(delta) < minDelta)
                    {
                        double sign = Math.Sign(parameterValue);
                        sign = sign == 0 ? 1 : sign;
                        delta = sign * minDelta;
                    }

                    parameters[j] = parameters[j] + delta;

                    // we only get error from function, but error(p + d) - error(p) = f(p + d) - f(p)
                    var errorVector = function(parameters);
                    errorVector = errorVector - errorVector0;

                    for (int i = 0; i < errorVector.Count; i++)
                        J[i, j] = errorVector[i] / delta;
                    parameters[j] = parameterValue; // restore original value
                }
                return J;
            }

            Function function;
        }

        static public void Test()
        {
            // generate x_i, y_i observations on test function

            var random = new Random();

            int n = 200;

            var X = Vector<double>.Build.Dense(n);
            var Y = Vector<double>.Build.Dense(n);

            {
                double a = 100; double b = 102;
                for (int i = 0; i < n; i++)
                {
                    double x = random.NextDouble() / (Math.PI / 4.0) - Math.PI / 8.0;
                    double y = a * Math.Cos(b * x) + b * Math.Sin(a * x) + random.NextDouble() * 0.1;
                    X[i] = x;
                    Y[i] = y;
                }
            }


            Function f = delegate (Vector<double> parameters)
            {
                var error = Vector<double>.Build.Dense(n);

                double a = parameters[0];
                double b = parameters[1];

                for (int i = 0; i < n; i++)
                {
                    double y = a * Math.Cos(b * X[i]) + b * Math.Sin(a * X[i]);
                    error[i] = Y[i] - y;
                }

                return error;
            };


            var levenbergMarquardt = new LevenbergMarquardt(f, null);

            var parameters0 = Vector<double>.Build.Dense(2);
            parameters0[0] = 90;
            parameters0[1] = 96;

            var rmsError = levenbergMarquardt.Minimize(parameters0);


        }
    } 
}