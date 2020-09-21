using Modules.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules
{
	class InternalJob
	{
		/// <summary>
		/// An identifier that can be used to see who or what created this job.
		/// </summary>
		public string Creator { get; }

		/// <summary>
		/// The start date of this job.
		/// </summary>
		public DateTime Start { get; private set; }

		/// <summary>
		/// How long this job will last from its start date.
		/// </summary>
		public TimeSpan Duration { get; }

		/// <summary>
		/// Wether this job is repeatable.
		/// </summary>
		public bool Repeat { get; }

		/// <summary>
		/// Job as given by the user.
		/// </summary>
		public IJob Job { get; }

		public InternalJob(IJob job, DateTime start)
		{
			this.Creator = job.Creator;
			this.Start = start;
			this.Duration = job.Duartion;
			this.Repeat = job.Repeat;
			this.Job = job;
		}

		/// <summary>
		/// Get the end date of this job.
		/// </summary>
		/// <returns></returns>
		public DateTime GetEnd()
		{
			return Start + Duration;
		}

		/// <summary>
		/// Restart this job.
		/// </summary>
		public void Restart()
		{
			Start = DateTime.UtcNow;
		}
	}
}
