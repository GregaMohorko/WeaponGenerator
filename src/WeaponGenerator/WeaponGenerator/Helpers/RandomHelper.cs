using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeaponGenerator.Helpers
{
	/// <summary>
	/// Thread-safe random number generator.
	/// </summary>
	static class RandomHelper
	{
		private static readonly Random random;
		private static readonly object lock_random;

		static RandomHelper()
		{
			random = new Random();
			lock_random = new object();
		}

		public static double Between(double min, double max)
		{
			lock(lock_random) {
				return random.NextDouble() * (max - min) + min;
			}
		}
	}
}
