using Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Jobs
{
	public class CheckupJob : IJob
	{
		public string Creator { get; set; }

		public TimeSpan Duartion { get; set; }

		public bool Repeat { get; set; }

		public string Character { get; set; }

		public CheckupJob(string creator, string character)
		{
			this.Creator = creator;
			this.Character = character;
			this.Repeat = false;
			this.Duartion = new TimeSpan(30, 0, 0, 0);
		}
	}
}
