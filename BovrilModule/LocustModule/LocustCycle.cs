using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BovrilModule
{
	public class LocustCycle
	{
		[JsonProperty]
		public string Region { get; private set; }

		[JsonProperty]
		public List<MoonInformation> Moons { get; private set; }

		[JsonConstructor]
		private LocustCycle()
		{

		}

		public LocustCycle(string region)
		{
			Region = region;
			Moons = new List<MoonInformation>();
		}

		public void AddMoon(MoonInformation moon)
		{
			Moons.Add(moon);
		}
	}
}
