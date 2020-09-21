using Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Jobs
{
	public class CalendarUpdateJob : IJob
	{
		public string Creator { get; set; }

		public TimeSpan Duartion { get; set; }

		public bool Repeat { get; set; } = true;

		public CalendarUpdateJob(string creator, TimeSpan duration)
		{
			this.Creator = creator;
			this.Duartion = duration;
		}
	}
}
