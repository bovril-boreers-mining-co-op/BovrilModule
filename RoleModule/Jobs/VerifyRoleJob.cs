using Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Jobs
{
	class VerifyRoleJob : IJob
	{
		public string Creator { get; set; }

		public TimeSpan Duartion { get; set; }

		public bool Repeat { get; set; }

		public VerifyRoleJob(string creator, TimeSpan duartion)
		{
			this.Creator = creator;
			this.Duartion = duartion;
			this.Repeat = true;
		}
	}
}
