using Modules.Interfaces;
using System;

namespace Modules.Jobs
{
	public class VerifyJob : IJob
	{
		public string Creator { get; }

		public TimeSpan Duartion { get; }

		public bool Repeat { get; }

		public VerifyJob(string creator, TimeSpan duartion)
		{
			this.Creator = creator;
			this.Duartion = duartion;
			this.Repeat = true;
		}
	}
}
