using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Counting
{
    class LogUtils
    {
        // This array holds logarithms of factorials, so _logFactorial[i] = log(i!)
        private static double[] _logFactorial = new double[] { 0 };

        /// <summary>
        /// Returns the logarithm of n choose k.
        /// </summary>
        /// <param name="n">The population size</param>
        /// <param name="k">The number of items to select</param>
        /// <returns>The logarithm of n choose k</returns>
        public static double LogNcK(int n, int k)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException("population size must be non-negative", "n");
            if (k < 0 || k > n)
                return double.NegativeInfinity;
            if (_logFactorial.Length <= n)
            {
                // Thread safe but potentially inefficient.  Not
                // sure which is worse, locks or this.
                var lf = _logFactorial;
                int start = lf.Length;
                Array.Resize(ref lf, n + 1);
                for (int i = start; i < lf.Length; ++i)
                    lf[i] = lf[i - 1] + Math.Log(i);
                _logFactorial = lf;
            }
            return _logFactorial[n] - _logFactorial[k] - _logFactorial[n - k];
        }

        /// <summary>
        /// Calculates the log of the sum of the exponentials of a given series of
        /// numbers.  This is a numerically stable alternative to to
        /// <c>Math.Log(a.Select(x => Math.Exp(x)).Sum())
        /// </summary>
        /// <param name="a">The numbers to "sum"</param>
        /// <returns>The log of the sum of the exponentials of <c>a</c></returns>
        public static double LogSumExp(params double[] a)
        {
            return LogSumExp((IEnumerable<double>)a);
        }

        public static double LogSumExp(IEnumerable<double> a)
        {
            // This does two passes over the number.
            double amax = a.Max();
            if (double.IsInfinity(amax))
                return amax;
            return Math.Log(a.Select(x => Math.Exp(x - amax)).Sum()) + amax;
        }
    }
}
