using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	public class SystemMoon
	{
		public string System { get; }

		public int Planet { get; }

		public int Moon { get; }

		public string Name
		{
			get
			{
				return $"{System} {ToRoman(Planet)} - Moon {Moon}";
			}
		}

		public SystemMoon(string system, int planet, int moon)
		{
			System = system;
			Planet = planet;
			Moon = moon;
		}

		public SystemMoon(SystemMoon systemMoon)
		{
			System = systemMoon.System;
			Planet = systemMoon.Planet;
			Moon = systemMoon.Moon;
		}

		/// <summary>
		/// https://stackoverflow.com/questions/7040289/converting-integers-to-roman-numerals
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		static string ToRoman(int number)
		{
			if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
			if (number < 1) return string.Empty;
			if (number >= 1000) return "M" + ToRoman(number - 1000);
			if (number >= 900) return "CM" + ToRoman(number - 900);
			if (number >= 500) return "D" + ToRoman(number - 500);
			if (number >= 400) return "CD" + ToRoman(number - 400);
			if (number >= 100) return "C" + ToRoman(number - 100);
			if (number >= 90) return "XC" + ToRoman(number - 90);
			if (number >= 50) return "L" + ToRoman(number - 50);
			if (number >= 40) return "XL" + ToRoman(number - 40);
			if (number >= 10) return "X" + ToRoman(number - 10);
			if (number >= 9) return "IX" + ToRoman(number - 9);
			if (number >= 5) return "V" + ToRoman(number - 5);
			if (number >= 4) return "IV" + ToRoman(number - 4);
			if (number >= 1) return "I" + ToRoman(number - 1);
			throw new ArgumentOutOfRangeException("something bad happened");
		}
	}
}
