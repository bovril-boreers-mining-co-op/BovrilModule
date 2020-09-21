using Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Jobs
{
	public class MoonJob : IJob
	{
		public string Creator { get; }

		public string Moon { get; }

		public TimeSpan Duartion { get; }

		public bool Repeat { get; }

		public MoonJob(string creator, string moon, TimeSpan duration)
		{
			this.Creator = creator;
			this.Moon = moon;
			this.Duartion = duration;
		}
	}
}
