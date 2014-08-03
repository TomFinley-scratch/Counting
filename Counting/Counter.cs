using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Counting
{
    using BitArray = System.Collections.BitArray;

    public class Counter
    {
        private readonly double[] _probs;
        private readonly double[] _logProbs;

        public int Length { get { return _probs.Length; } }

        public Counter(params double[] probs)
        {
            if (probs == null || probs.Length == 0)
                throw new ArgumentException("must have some values", "probs");
            double psum = probs.Sum();
            _probs = probs.Select(p => p / psum).ToArray();
            for (int i = 0; i < _probs.Length; ++i)
            {
                if (!(_probs[i] > 0))
                {
                    throw new ArgumentOutOfRangeException(string.Format(
                        "Probability[{0}] = {1} normalized to non-positive number", i, probs[i]), "probs");
                }
            }
            _logProbs = _probs.Select(p => Math.Log(p)).ToArray();
        }

        public double LogP(BitArray b, int c)
        {
            int stillSet = Enumerable.Range(0, b.Length).Where(i => b[i]).Count();
            return LogP(b, c, stillSet, 0);
        }

        /// <summary>
        /// Helper method for calculating the log probability of b given c. This function
        /// operates recursively.
        /// </summary>
        /// <param name="b">The set bit pattern.</param>
        /// <param name="c">The generating number of values.</param>
        /// <param name="stillSet">A convenience quantity indicating the number of set
        /// bits in b from b[i:Length]</param>
        /// <param name="i">The bucket this current call corresponds to</param>
        /// <returns>The logarithm of the probability</returns>
        private double LogP(BitArray b, int c, int stillSet, int i)
        {
            if (i >= Length)
                return c == 0 ? 0 : Double.NegativeInfinity;
            if (b[i])
            {
                // We will have stillSet - 1 remaining after accounting for this,
                // so we can only have up to c - (stillSet - 1) values in this bin.
                IEnumerable<double> values = Enumerable.Range(1, c - stillSet + 1)
                    .Select(j => LogUtils.LogNcK(c, j) + j * _logProbs[i] + LogP(b, c - j, stillSet - 1, i + 1));
                double[] v = values.ToArray();
                double result = LogUtils.LogSumExp(v);
                if (double.IsNaN(result))
                {
                    string msg = string.Format("Warning: LogSumExp({0}) = {1} for c={2}, i={3}",
                        string.Join(", ", v), result, c, i);
                    throw new Exception(msg);
                }
                return result;
            }
            else
                return LogP(b, c, stillSet, i + 1);
        }

        public double LogP2(BitArray b, int c)
        {
            int stillSet = Enumerable.Range(0, b.Length).Where(i => b[i]).Count();
            double[] pforward = new double[c + 1];
            for (int i = 1; i < pforward.Length; ++i)
                pforward[i] = Double.NegativeInfinity;
            double[] pcurrent = new double[c + 1];
            double[] work = new double[c + 1];

            for (int i = b.Length - 1; i >= 0; --i)
            {
                if (b[i])
                {
                    pcurrent[0] = Double.NegativeInfinity;
                    for (int cc = 1; cc <= c; ++cc)
                    {
                        // We can use somewhere between [1, cc] items.
                        for (int j = 1; j <= cc; ++j)
                            work[j - 1] = LogUtils.LogNcK(cc, j) + j * _logProbs[i] + pforward[cc - j];
                        pcurrent[cc] = LogUtils.LogSumExp(work.Take(cc));
                    }
                    var ptemp = pcurrent;
                    pcurrent = pforward;
                    pforward = ptemp;
                }
            }
            return pforward[c];
        }

        /// <summary>
        /// Generate a sample bit-filter from the counter, given a number of elements.
        /// </summary>
        /// <param name="c">The number of unique elements.</param>
        /// <param name="rgen">A random number generator, or <c>null</c> if you want this method to generate its own</param>
        /// <returns>A randomly generated bit-filter corresponding to <c>c</c> unique items</returns>
        public BitArray Sample(int c, Random rgen = null)
        {
            if (rgen == null)
                rgen = new Random();
            BitArray b = new BitArray(Length);
            while (c-- > 0)
            {
                int bin = 0;
                double p = rgen.NextDouble();
                while (bin < _probs.Length && p >= _probs[bin])
                    p -= _probs[bin++];
                b[bin] = true;
            }
            return b;
        }
    }
}
