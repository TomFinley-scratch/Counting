using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Counting;

namespace CountingTest
{
    using BitArray = System.Collections.BitArray;

    [TestClass]
    public class CountingTest
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

        [TestMethod]
        public void SampleCheck()
        {
            // A probabilistic check.
            double[] p = new double[] { 1, 1, 2, 4, 8, 16, 32, 64 };
            Counter counter = new Counter(p);
            Random rgen = new Random(0);
            const int trials = 100;
            const int count = 10;
            int exceed = 0;
            for (int i = 0; i < trials; ++i)
            {
                BitArray sample = counter.Sample(count, rgen);
                double calcProb = Math.Exp(counter.LogP2(sample, count));

                const int resample = 100000;
                int resampleEquals = 0;
                for (int j = 0; j < resample; ++j)
                    if (BEquals(counter.Sample(count, rgen), sample))
                        resampleEquals++;

                double sampleProb = (double)resampleEquals / resample;
                double stddev = Math.Sqrt(calcProb * (1 - calcProb) / resample);
                double z = (sampleProb - calcProb) / stddev;
                if (Math.Abs(z) > 1.96)
                    exceed++;

                Console.WriteLine("{0}: calc {1:0.00000}, sample {2:0.00000} ({3:+0.000;-0.000})", BString(sample), calcProb, sampleProb, z);
            }

            Console.WriteLine("{0} of {1} were 5% unlikely", exceed, trials);
            Assert.IsTrue(exceed <= 0.1 * trials, "unlikely events in sample, check correctness");
        }
    }
}
