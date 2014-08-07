using System;
using System.Linq;

namespace Counting
{
    using BitArray = System.Collections.BitArray;

    public class Counter
    {
        // The probabilities.
        private readonly double[] _probs;
        // The log of the probabilities.
        private readonly double[] _logProbs;

        public int Length { get { return _probs.Length; } }

        /// <summary>
        /// Create a new counter object.
        /// </summary>
        /// <param name="likelihoods">The proportional likelihoods of sampling each.
        /// These must be positive finite values.</param>
        public Counter(params double[] likelihoods)
        {
            if (likelihoods == null || likelihoods.Length == 0)
                throw new ArgumentException("must have some values", "probs");
            double psum = likelihoods.Sum();
            _probs = likelihoods.Select(p => p / psum).ToArray();
            for (int i = 0; i < _probs.Length; ++i)
            {
                if (!(_probs[i] > 0))
                {
                    throw new ArgumentOutOfRangeException(string.Format(
                        "Probability[{0}] = {1} normalized to non-positive number", i, likelihoods[i]), "probs");
                }
            }
            _logProbs = _probs.Select(p => Math.Log(p)).ToArray();
        }

        /// <summary>
        /// Helper method for calculating the log probability of b given c.
        /// </summary>
        /// <param name="b">The set bit pattern</param>
        /// <param name="c">The generating number of values</param>
        /// <returns>The logarithm of the probability that bit-pattern b
        /// arose given c unique items</returns>
        public double LogP(BitArray b, int c)
        {
            int stillSet = Enumerable.Range(0, b.Length).Where(i => b[i]).Count();
            double[] pforward = new double[c + 1];
            for (int i = 1; i < pforward.Length; ++i)
                pforward[i] = Double.NegativeInfinity;
            double[] pcurrent = new double[c + 1];
            double[] work = new double[c + 1];

            for (int i = b.Length - 1; i >= 0; --i)
            {
                if (!b[i])
                    continue;
                pcurrent[0] = Double.NegativeInfinity;
                for (int cc = 1; cc <= c; ++cc)
                {
                    // We can use somewhere between [1, cc] items. Sum over each possible value j.
                    for (int j = 1; j <= cc; ++j)
                        work[j - 1] = LogUtils.LogNcK(cc, j) + j * _logProbs[i] + pforward[cc - j];
                    pcurrent[cc] = LogUtils.LogSumExp(work.Take(cc));
                }
                var ptemp = pcurrent;
                pcurrent = pforward;
                pforward = ptemp;
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
