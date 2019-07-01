using NModule.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace RecruitmentModule
{
	class NameCheckerJob : IJob
	{
		public string Creator { get; }

		public DateTime Start { get; }

		public NameCheckerJob(string creator, DateTime start)
		{
			Creator = creator;
			Start = start;
		}

		public int CompareTo(IJob other)
		{
			return Start.CompareTo(other.Start);
		}
	}
}
