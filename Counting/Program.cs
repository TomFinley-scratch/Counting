using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Counting
{
    using BitArray = System.Collections.BitArray;

    class Counter
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

    class Program
    {
        private static string BString(BitArray b)
        {
            StringBuilder sb = new StringBuilder(b.Length);
            for (int i = 0; i < b.Length; ++i)
                sb.Append(b[i] ? '1' : '0');
            return sb.ToString();
        }

        private static bool BEquals(BitArray a, BitArray b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < b.Length; ++i)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        private static void TestCounter()
        {
            double[] p = new double[] { 1, 1, 2, 4, 8, 16, 32, 64 };
            Counter counter = new Counter(p);
            Random rgen = new Random(0);
            const int trials = 20;
            const int count = 10;
            for (int i = 0; i < trials; ++i)
            {
                BitArray sample = counter.Sample(count, rgen);
                double pp = Math.Exp(counter.LogP(sample, count));

                const int resample = 100000;
                int resampleEquals = 0;
                for (int j = 0; j < resample; ++j)
                    if (BEquals(counter.Sample(count, rgen), sample))
                        resampleEquals++;
                Console.WriteLine("{0}: {1}, {2}", BString(sample), pp, (double)resampleEquals / resample);
            }
        }

        private static void Compare(int n, int k)
        {
            var lognck = LogUtils.LogNcK(n, k);
            double e = (double)k / n;
            var stirling = -n * (e * Math.Log(e) + (1 - e) * Math.Log(1 - e));
            Console.WriteLine("{0} vs {1}", lognck, stirling);
        }

        static void Main(string[] args)
        {
            Compare(100, 10);
            Compare(1000, 100);
            Compare(1000, 300);
            Compare(10000, 3000);
        }
    }
}
